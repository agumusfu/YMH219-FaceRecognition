using OpenCvSharp;

namespace FaceRecognition.Models;

/// <summary>
/// Algılanan tek bir yüz bölgesini, tahmin edilen etiketi ve güven skorunu temsil eder.
/// </summary>
public class FaceDetectionResult
{
    public Rect BoundingBox { get; set; }
    public string Label { get; set; } = "Bilinmeyen";
    public double Confidence { get; set; } // 0.0 - 1.0 arası (yüzde olarak gösterilir)
}
