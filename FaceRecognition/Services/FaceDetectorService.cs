using System.Reflection;
using OpenCvSharp;

namespace FaceRecognition.Services;

/// <summary>
/// OpenCV Haar Cascade kullanarak görüntü veya kamera karesi üzerinde çoklu yüz algılama yapan servis.
/// </summary>
public class FaceDetectorService : IDisposable
{
    private CascadeClassifier? _cascade;
    private bool _disposed;

    public bool IsLoaded => _cascade != null && !_cascade.Empty();

    public (bool Success, string Message) Initialize()
    {
        try
        {
            string localPath = Path.Combine(AppContext.BaseDirectory, "Models", "haarcascade_frontalface_default.xml");
            if (!File.Exists(localPath))
            {
                // Tek dosya publish durumunda gömülü kaynaktan çıkar
                localPath = ExtractEmbeddedCascade();
            }

            if (!File.Exists(localPath))
            {
                return (false, "Yüz algılama model dosyası (haarcascade_frontalface_default.xml) bulunamadı.");
            }

            _cascade?.Dispose();
            _cascade = new CascadeClassifier(localPath);
            if (_cascade.Empty())
            {
                return (false, "Haar Cascade model dosyası yüklenemedi veya geçersiz.");
            }

            return (true, "Yüz algılama modeli başarıyla yüklendi.");
        }
        catch (Exception ex)
        {
            return (false, $"Model yüklenirken hata oluştu: {ex.Message}");
        }
    }

    public Rect[] DetectFaces(Mat frame)
    {
        if (_cascade == null || _cascade.Empty() || frame.Empty())
            return Array.Empty<Rect>();

        using var gray = new Mat();
        if (frame.Channels() > 1)
            Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
        else
            frame.CopyTo(gray);

        Cv2.EqualizeHist(gray, gray);

        return _cascade.DetectMultiScale(
            image: gray,
            scaleFactor: 1.15,
            minNeighbors: 5,
            flags: HaarDetectionTypes.ScaleImage,
            minSize: new OpenCvSharp.Size(45, 45));
    }

    private static string ExtractEmbeddedCascade()
    {
        string tempDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FaceRecognition", "Models");
        Directory.CreateDirectory(tempDir);
        string targetPath = Path.Combine(tempDir, "haarcascade_frontalface_default.xml");

        if (File.Exists(targetPath)) return targetPath;

        var asm = Assembly.GetExecutingAssembly();
        string? resName = asm.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith("haarcascade_frontalface_default.xml", StringComparison.OrdinalIgnoreCase));

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
        _cascade?.Dispose();
        GC.SuppressFinalize(this);
    }
}
