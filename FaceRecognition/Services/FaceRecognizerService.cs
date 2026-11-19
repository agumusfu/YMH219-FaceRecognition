using OpenCvSharp;

namespace FaceRecognition.Services;

/// <summary>
/// Yerel gönüllü örnek kümeleri üzerinden sıfır-ortalamalı (Zero-Mean) Pearson korelasyonu ile
/// yüz tanıma/etiketleme, güven skoru hesaplama ve örnek silme/yönetim servisi.
/// </summary>
public class FaceRecognizerService
{
    private const int FeatureSize = 64;
    private const double RecognitionThreshold = 0.56;

    private readonly string _trainingDir;
    private readonly string _savedFacesDir;
    private readonly Dictionary<string, List<float[]>> _classVectors = new(StringComparer.OrdinalIgnoreCase);

    public string TrainingDirectory => _trainingDir;
    public string SavedFacesDirectory => _savedFacesDir;
    public int ClassCount => _classVectors.Count;
    public int TotalSampleCount => _classVectors.Values.Sum(v => v.Count);

    public FaceRecognizerService()
    {
        string baseDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FaceRecognition");
        _trainingDir = Path.Combine(baseDir, "TrainingData");
        _savedFacesDir = Path.Combine(baseDir, "SavedFaces");
        Directory.CreateDirectory(_trainingDir);
        Directory.CreateDirectory(_savedFacesDir);
    }

    public IReadOnlyDictionary<string, int> GetClassSummary()
        => _classVectors.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count);

    public void LoadTrainingData()
    {
        _classVectors.Clear();
        if (!Directory.Exists(_trainingDir)) return;

        foreach (string dir in Directory.GetDirectories(_trainingDir))
        {
            string label = Path.GetFileName(dir);
            var list = new List<float[]>();

            foreach (string file in Directory.GetFiles(dir, "*.*")
                .Where(f => f.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                         || f.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase)))
            {
                using var img = Cv2.ImRead(file, ImreadModes.Grayscale);
                if (!img.Empty())
                {
                    list.Add(ExtractFeatureVector(img));
                }
            }

            if (list.Count > 0)
            {
                _classVectors[label] = list;
            }
        }
    }

    public (string Label, double Confidence) Predict(Mat faceCrop)
    {
        if (_classVectors.Count == 0 || faceCrop.Empty())
            return ("Egitimsiz (Ornek Yok)", 0.0);

        float[] probe = ExtractFeatureVector(faceCrop);
        string bestLabel = "Bilinmeyen";
        double bestScore = -1.0;

        foreach (var kvp in _classVectors)
        {
            double maxClassScore = -1.0;
            foreach (var sample in kvp.Value)
            {
                double sim = PearsonSimilarity(probe, sample);
                if (sim > maxClassScore) maxClassScore = sim;
            }

            if (maxClassScore > bestScore)
            {
                bestScore = maxClassScore;
                bestLabel = kvp.Key;
            }
        }

        double displayScore = Math.Clamp(bestScore, 0.0, 1.0);
        if (bestScore < RecognitionThreshold)
        {
            return ("Bilinmeyen", displayScore);
        }

        return (bestLabel, displayScore);
    }

    public string AddTrainingSample(string label, Mat faceCrop)
    {
        string cleanLabel = string.Concat(label.Trim().Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(cleanLabel))
            throw new ArgumentException("Geçerli bir etiket adı girilmelidir.");

        string labelDir = Path.Combine(_trainingDir, cleanLabel);
        Directory.CreateDirectory(labelDir);

        string filePath = Path.Combine(labelDir, $"sample_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        using var resized = PrepareGrayFace(faceCrop);
        Cv2.ImWrite(filePath, resized);

        if (!_classVectors.TryGetValue(cleanLabel, out var list))
        {
            list = new List<float[]>();
            _classVectors[cleanLabel] = list;
        }
        list.Add(ExtractFeatureVector(resized));
        return filePath;
    }

    public bool DeleteClass(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return false;
        bool removed = _classVectors.Remove(label);
        string dir = Path.Combine(_trainingDir, label);
        if (Directory.Exists(dir))
        {
            Directory.Delete(dir, recursive: true);
            removed = true;
        }
        return removed;
    }

    public void DeleteAllTrainingData()
    {
        _classVectors.Clear();
        if (Directory.Exists(_trainingDir))
        {
            foreach (string dir in Directory.GetDirectories(_trainingDir))
                Directory.Delete(dir, recursive: true);
        }
        if (Directory.Exists(_savedFacesDir))
        {
            foreach (string file in Directory.GetFiles(_savedFacesDir))
                File.Delete(file);
        }
    }

    public string SaveDetectedFace(Mat faceCrop, string label)
    {
        string safeLabel = string.Concat(label.Trim().Split(Path.GetInvalidFileNameChars()));
        if (string.IsNullOrWhiteSpace(safeLabel)) safeLabel = "Yuz";

        Directory.CreateDirectory(_savedFacesDir);
        string filePath = Path.Combine(_savedFacesDir, $"{safeLabel}_{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
        Cv2.ImWrite(filePath, faceCrop);
        return filePath;
    }

    private static Mat PrepareGrayFace(Mat input)
    {
        using var gray = new Mat();
        if (input.Channels() > 1)
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);
        else
            input.CopyTo(gray);

        // Arka plan gürültüsünü azaltmak için merkez %80 yüz bölgesini al
        int padX = Math.Max(0, (int)(gray.Width * 0.10));
        int padY = Math.Max(0, (int)(gray.Height * 0.10));
        int w = Math.Max(1, gray.Width - 2 * padX);
        int h = Math.Max(1, gray.Height - 2 * padY);
        using var cropped = new Mat(gray, new Rect(padX, padY, w, h));

        var resized = new Mat();
        Cv2.Resize(cropped, resized, new OpenCvSharp.Size(FeatureSize, FeatureSize));
        Cv2.EqualizeHist(resized, resized);
        return resized;
    }

    private static float[] ExtractFeatureVector(Mat input)
    {
        using var normalized = PrepareGrayFace(input);
        int len = FeatureSize * FeatureSize;
        float[] vec = new float[len];

        double sum = 0;
        for (int y = 0; y < FeatureSize; y++)
        {
            for (int x = 0; x < FeatureSize; x++)
            {
                float val = normalized.At<byte>(y, x) / 255.0f;
                vec[y * FeatureSize + x] = val;
                sum += val;
            }
        }

        // Zero-Mean (ortalama çıkarma) -> rastgele/alakasız görüntülerde yüksek skor çıkmasını engeller
        float mean = (float)(sum / len);
        double normSq = 0;
        for (int i = 0; i < len; i++)
        {
            vec[i] -= mean;
            normSq += vec[i] * vec[i];
        }

        float norm = (float)Math.Sqrt(normSq);
        if (norm > 1e-6f)
        {
            for (int i = 0; i < len; i++) vec[i] /= norm;
        }

        return vec;
    }

    private static double PearsonSimilarity(float[] a, float[] b)
    {
        double dot = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
        }
        return dot;
    }
}
