using System.Reflection;
using OpenCvSharp;

namespace FaceRecognition.Services;

/// <summary>
/// OpenCV Haar Cascade kullanarak görüntü veya kamera karesi üzerinde çoklu yüz algılama yapan servis.
/// Kontrast (standart sapma) ve göz bölgesi doğrulaması ile yüz dışı yanlış algılamaları (false-positive) eler.
/// </summary>
public class FaceDetectorService : IDisposable
{
    private CascadeClassifier? _faceCascade;
    private CascadeClassifier? _eyeCascade;
    private bool _disposed;

    public bool IsLoaded => _faceCascade != null && !_faceCascade.Empty();

    public (bool Success, string Message) Initialize()
    {
        try
        {
            string facePath = ResolveModelPath("haarcascade_frontalface_default.xml");
            if (!File.Exists(facePath))
            {
                return (false, "Yüz algılama model dosyası (haarcascade_frontalface_default.xml) bulunamadı.");
            }

            _faceCascade?.Dispose();
            _faceCascade = new CascadeClassifier(facePath);
            if (_faceCascade.Empty())
            {
                return (false, "Haar Cascade yüz model dosyası yüklenemedi veya geçersiz.");
            }

            string eyePath = ResolveModelPath("haarcascade_eye_tree_eyeglasses.xml");
            if (File.Exists(eyePath))
            {
                _eyeCascade?.Dispose();
                _eyeCascade = new CascadeClassifier(eyePath);
            }

            return (true, "Yüz ve göz doğrulama modelleri başarıyla yüklendi.");
        }
        catch (Exception ex)
        {
            return (false, $"Model yüklenirken hata oluştu: {ex.Message}");
        }
    }

    public Rect[] DetectFaces(Mat frame)
    {
        if (_faceCascade == null || _faceCascade.Empty() || frame.Empty())
            return Array.Empty<Rect>();

        using var gray = new Mat();
        if (frame.Channels() > 1)
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        else
            frame.CopyTo(gray);

        Cv2.EqualizeHist(gray, gray);

        int minSide = Math.Max(60, Math.Min(frame.Width, frame.Height) / 10);
        Rect[] candidates = _faceCascade.DetectMultiScale(
            image: gray,
            scaleFactor: 1.1,
            minNeighbors: 6,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new OpenCvSharp.Size(minSide, minSide));

        var verified = new List<Rect>();
        foreach (Rect rect in candidates)
        {
            Rect safe = rect & new Rect(0, 0, gray.Width, gray.Height);
            if (safe.Width < 50 || safe.Height < 50) continue;

            using var faceRoi = new Mat(gray, safe);

            // 1. Kontrast kontrolü: Düz/karanlık yüzeyleri (ceket yakası, gölge vb.) ele
            Cv2.MeanStdDev(faceRoi, out _, out Scalar stddev);
            if (stddev.Val0 < 20.0) continue;

            // 2. Göz bölgesi doğrulaması: Yüzün üst %65'lik alanında en az bir göz yapısı aranır
            if (_eyeCascade != null && !_eyeCascade.Empty())
            {
                Rect upperHalf = new Rect(0, 0, faceRoi.Width, (int)(faceRoi.Height * 0.65));
                using var upperRoi = new Mat(faceRoi, upperHalf);
                int minEye = Math.Max(14, faceRoi.Width / 8);
                Rect[] eyes = _eyeCascade.DetectMultiScale(
                    image: upperRoi,
                    scaleFactor: 1.1,
                    minNeighbors: 2,
                    flags: HaarDetectionTypes.ScaleImage,
                    minSize: new OpenCvSharp.Size(minEye, minEye));

                if (eyes.Length == 0) continue;
            }

            verified.Add(safe);
        }

        return verified.ToArray();
    }

    private static string ResolveModelPath(string fileName)
    {
        string localPath = Path.Combine(AppContext.BaseDirectory, "Models", fileName);
        if (File.Exists(localPath)) return localPath;
        return ExtractEmbeddedResource(fileName);
    }

    private static string ExtractEmbeddedResource(string fileName)
    {
        string tempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FaceRecognition", "Models");
        Directory.CreateDirectory(tempDir);
        string targetPath = Path.Combine(tempDir, fileName);

        if (File.Exists(targetPath)) return targetPath;

        var asm = Assembly.GetExecutingAssembly();
        string? resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        if (resName == null) return string.Empty;

        using var stream = asm.GetManifestResourceStream(resName);
        if (stream == null) return string.Empty;

        using var fs = new FileStream(targetPath, FileMode.Create, FileAccess.Write);
        stream.CopyTo(fs);
        return targetPath;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _faceCascade?.Dispose();
        _eyeCascade?.Dispose();
        GC.SuppressFinalize(this);
    }
}
