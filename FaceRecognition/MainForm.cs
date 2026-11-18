using FaceRecognition.Models;
using FaceRecognition.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;

namespace FaceRecognition;

public partial class MainForm : Form
{
    private readonly FaceDetectorService _detector = new();
    private readonly FaceRecognizerService _recognizer = new();
    private VideoCapture? _capture;
    private CancellationTokenSource? _camCts;
    private Mat? _currentRawFrame;
    private Rect[] _currentFaces = Array.Empty<Rect>();
    private readonly object _frameLock = new();

    public MainForm()
    {
        InitializeComponent();
        try
        {
            if (!string.IsNullOrEmpty(Environment.ProcessPath))
                this.Icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath);
        }
        catch { }
    }

    private void MainForm_Load(object? sender, EventArgs e)
    {
        var (ok, msg) = _detector.Initialize();
        if (!ok)
        {
            tsslModel.Text = "Model: Hata";
            MessageBox.Show(msg, "Model Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        else
        {
            tsslModel.Text = "Model: Haar Cascade Hazır";
        }

        _recognizer.LoadTrainingData();
        RefreshClassList();
    }

    // ─── Kamera Kontrolü ─────────────────────────────────────────────────────

    private void btnStartCam_Click(object? sender, EventArgs e)
    {
        if (!_detector.IsLoaded)
        {
            MessageBox.Show("Yüz algılama modeli yüklü değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            _capture = new VideoCapture(0, VideoCaptureAPIs.DSHOW);
            if (!_capture.IsOpened())
            {
                _capture.Dispose();
                _capture = new VideoCapture(0);
            }

            if (!_capture.IsOpened())
            {
                _capture.Dispose();
                _capture = null;
                MessageBox.Show("Kamera aygıtı bulunamadı veya başka bir uygulama tarafından kullanılıyor.",
                    "Kamera Hatası", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnStartCam.Enabled = false;
            btnStopCam.Enabled = true;
            lblStatus.Text = "Durum: Kamera Aktif";
            lblStatus.ForeColor = Color.LimeGreen;

            _camCts = new CancellationTokenSource();
            _ = CameraLoopAsync(_camCts.Token);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Kamera başlatılamadı: {ex.Message}", "Kamera Hatası", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnStopCam_Click(object? sender, EventArgs e)
    {
        StopCamera();
    }

    private void StopCamera()
    {
        try { _camCts?.Cancel(); } catch { }
        try { _capture?.Release(); _capture?.Dispose(); } catch { }
        _capture = null;

        if (!IsDisposed && !Disposing)
        {
            btnStartCam.Enabled = true;
            btnStopCam.Enabled = false;
            lblStatus.Text = "Durum: Kamera Durduruldu";
            lblStatus.ForeColor = Color.LightSkyBlue;
        }
    }

    private async Task CameraLoopAsync(CancellationToken token)
    {
        using var frame = new Mat();
        while (!token.IsCancellationRequested && _capture != null && _capture.IsOpened())
        {
            try
            {
                if (!_capture.Read(frame) || frame.Empty())
                {
                    await Task.Delay(30, token);
                    continue;
                }

                ProcessAndDisplayFrame(frame);
                await Task.Delay(33, token);
            }
            catch (OperationCanceledException) { break; }
            catch { await Task.Delay(50, token); }
        }
    }

    // ─── Görüntü Dosyası Seçme ───────────────────────────────────────────────

    private void btnLoadImage_Click(object? sender, EventArgs e)
    {
        if (!_detector.IsLoaded)
        {
            MessageBox.Show("Yüz algılama modeli yüklü değil.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        StopCamera();

        using var dlg = new OpenFileDialog
        {
            Filter = "Resim Dosyaları (*.jpg;*.jpeg;*.png;*.bmp)|*.jpg;*.jpeg;*.png;*.bmp"
        };

        if (dlg.ShowDialog() != DialogResult.OK) return;

        try
        {
            using var img = Cv2.ImRead(dlg.FileName, ImreadModes.Color);
            if (img.Empty())
            {
                MessageBox.Show("Seçilen görüntü dosyası okunamadı.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ProcessAndDisplayFrame(img);
            lblStatus.Text = $"Durum: Görüntü ({Path.GetFileName(dlg.FileName)})";
            lblStatus.ForeColor = Color.Cyan;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Görüntü işlenirken hata oluştu: {ex.Message}", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    // ─── Algılama & Tanıma ───────────────────────────────────────────────────

    private void ProcessAndDisplayFrame(Mat sourceFrame)
    {
        Rect[] faces = _detector.DetectFaces(sourceFrame);
        var results = new List<FaceDetectionResult>();

        using var annotated = sourceFrame.Clone();
        foreach (Rect rect in faces)
        {
            Rect safeRect = rect & new Rect(0, 0, sourceFrame.Width, sourceFrame.Height);
            if (safeRect.Width <= 0 || safeRect.Height <= 0) continue;

            using var crop = new Mat(sourceFrame, safeRect);
            var (label, conf) = _recognizer.Predict(crop);

            results.Add(new FaceDetectionResult
            {
                BoundingBox = safeRect,
                Label = label,
                Confidence = conf
            });

            Scalar boxColor = label == "Bilinmeyen" || label.StartsWith("Egitimsiz")
                ? new Scalar(0, 200, 255) // Sarı-turuncu
                : new Scalar(0, 230, 100); // Yeşil

            Cv2.Rectangle(annotated, safeRect, boxColor, 2);

            string tag = conf > 0 ? $"{label} ({conf:P0})" : label;
            int textY = Math.Max(20, safeRect.Y - 8);
            Cv2.PutText(annotated, tag, new OpenCvSharp.Point(safeRect.X, textY),
                HersheyFonts.HersheySimplex, 0.65, boxColor, 2);
        }

        lock (_frameLock)
        {
            _currentRawFrame?.Dispose();
            _currentRawFrame = sourceFrame.Clone();
            _currentFaces = faces;
        }

        Bitmap bmp = BitmapConverter.ToBitmap(annotated);

        if (IsDisposed || Disposing) { bmp.Dispose(); return; }

        void UpdateUI()
        {
            if (IsDisposed || Disposing) { bmp.Dispose(); return; }
            var old = pbPreview.Image;
            pbPreview.Image = bmp;
            old?.Dispose();

            tsslFaceCount.Text = $"Algılanan Yüz: {results.Count}";
            lstDetections.Items.Clear();
            for (int i = 0; i < results.Count; i++)
            {
                var r = results[i];
                string scoreText = r.Confidence > 0 ? $"{r.Confidence:P1}" : "-";
                lstDetections.Items.Add($"#{i + 1} {r.Label} | Güven: {scoreText} | {r.BoundingBox.Width}x{r.BoundingBox.Height}");
            }
        }

        if (InvokeRequired)
            try { Invoke(UpdateUI); } catch { bmp.Dispose(); }
        else
            UpdateUI();
    }

    // ─── Yerel Eğitim & Yüz Kaydetme ─────────────────────────────────────────

    private void btnAddSample_Click(object? sender, EventArgs e)
    {
        string label = txtLabel.Text.Trim();
        if (string.IsNullOrWhiteSpace(label))
        {
            MessageBox.Show("Lütfen bir kişi etiketi girin (ör. Ahmet).", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        lock (_frameLock)
        {
            if (_currentRawFrame == null || _currentRawFrame.Empty() || _currentFaces.Length == 0)
            {
                MessageBox.Show("Örnek eklemek için önce kamera veya görüntü üzerinde en az bir yüz algılanmalıdır.",
                    "Yüz Bulunamadı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            Rect r = _currentFaces[0] & new Rect(0, 0, _currentRawFrame.Width, _currentRawFrame.Height);
            using var crop = new Mat(_currentRawFrame, r);
            _recognizer.AddTrainingSample(label, crop);
        }

        RefreshClassList();
        MessageBox.Show($"'{label}' etiketi için yeni yüz örneği kaydedildi ve modele eklendi.",
            "Örnek Eklendi", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void btnSaveFaces_Click(object? sender, EventArgs e)
    {
        int savedCount = 0;
        lock (_frameLock)
        {
            if (_currentRawFrame == null || _currentRawFrame.Empty() || _currentFaces.Length == 0)
            {
                MessageBox.Show("Kaydedilecek algılanmış yüz bulunmuyor.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (Rect rect in _currentFaces)
            {
                Rect r = rect & new Rect(0, 0, _currentRawFrame.Width, _currentRawFrame.Height);
                if (r.Width <= 0 || r.Height <= 0) continue;
                using var crop = new Mat(_currentRawFrame, r);
                var (label, _) = _recognizer.Predict(crop);
                _recognizer.SaveDetectedFace(crop, label);
                savedCount++;
            }
        }

        MessageBox.Show($"{savedCount} adet yüz kırpılarak kaydedildi:\n{_recognizer.SavedFacesDirectory}",
            "Yüzler Kaydedildi", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void RefreshClassList()
    {
        lstClasses.Items.Clear();
        var summary = _recognizer.GetClassSummary();
        foreach (var kvp in summary)
        {
            lstClasses.Items.Add($"{kvp.Key} ({kvp.Value} örnek)");
        }
        tsslClassCount.Text = $"Kayıtlı Sınıf: {_recognizer.ClassCount} ({_recognizer.TotalSampleCount} örnek)";
    }

    private void MainForm_FormClosing(object? sender, FormClosingEventArgs e)
    {
        StopCamera();
        lock (_frameLock)
        {
            _currentRawFrame?.Dispose();
            _currentRawFrame = null;
        }
        _detector.Dispose();
    }
}
