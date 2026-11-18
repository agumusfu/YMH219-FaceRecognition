namespace FaceRecognition;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.pnlTop = new Panel();
        this.btnStartCam = new Button();
        this.btnStopCam = new Button();
        this.btnLoadImage = new Button();
        this.btnSaveFaces = new Button();
        this.lblStatus = new Label();

        this.splitMain = new SplitContainer();
        this.pbPreview = new PictureBox();

        this.pnlRight = new Panel();
        this.grpTrain = new GroupBox();
        this.lblLabel = new Label();
        this.txtLabel = new TextBox();
        this.btnAddSample = new Button();
        this.lstClasses = new ListBox();
        this.grpResults = new GroupBox();
        this.lstDetections = new ListBox();

        this.statusStrip = new StatusStrip();
        this.tsslModel = new ToolStripStatusLabel();
        this.tsslFaceCount = new ToolStripStatusLabel();
        this.tsslClassCount = new ToolStripStatusLabel();

        // pnlTop
        pnlTop.Dock = DockStyle.Top;
        pnlTop.Height = 56;
        pnlTop.BackColor = Color.FromArgb(32, 35, 48);
        pnlTop.Padding = new Padding(10);

        btnStartCam.Text = "Kamera Başlat";
        btnStartCam.Location = new Point(12, 10);
        btnStartCam.Size = new Size(135, 36);
        btnStartCam.BackColor = Color.FromArgb(0, 150, 100);
        btnStartCam.ForeColor = Color.White;
        btnStartCam.FlatStyle = FlatStyle.Flat;
        btnStartCam.Click += btnStartCam_Click;
        pnlTop.Controls.Add(btnStartCam);

        btnStopCam.Text = "Kamera Durdur";
        btnStopCam.Location = new Point(155, 10);
        btnStopCam.Size = new Size(135, 36);
        btnStopCam.BackColor = Color.FromArgb(180, 50, 50);
        btnStopCam.ForeColor = Color.White;
        btnStopCam.FlatStyle = FlatStyle.Flat;
        btnStopCam.Enabled = false;
        btnStopCam.Click += btnStopCam_Click;
        pnlTop.Controls.Add(btnStopCam);

        btnLoadImage.Text = "Görüntü Seç";
        btnLoadImage.Location = new Point(298, 10);
        btnLoadImage.Size = new Size(125, 36);
        btnLoadImage.BackColor = Color.FromArgb(50, 110, 200);
        btnLoadImage.ForeColor = Color.White;
        btnLoadImage.FlatStyle = FlatStyle.Flat;
        btnLoadImage.Click += btnLoadImage_Click;
        pnlTop.Controls.Add(btnLoadImage);

        btnSaveFaces.Text = "Yüzleri Kaydet";
        btnSaveFaces.Location = new Point(431, 10);
        btnSaveFaces.Size = new Size(130, 36);
        btnSaveFaces.BackColor = Color.FromArgb(0, 140, 130);
        btnSaveFaces.ForeColor = Color.White;
        btnSaveFaces.FlatStyle = FlatStyle.Flat;
        btnSaveFaces.Click += btnSaveFaces_Click;
        pnlTop.Controls.Add(btnSaveFaces);

        lblStatus.Text = "Durum: Hazır";
        lblStatus.Font = new Font("Segoe UI", 10f, FontStyle.Bold);
        lblStatus.ForeColor = Color.LightSkyBlue;
        lblStatus.Location = new Point(600, 17);
        lblStatus.AutoSize = true;
        pnlTop.Controls.Add(lblStatus);

        // splitMain
        splitMain.Dock = DockStyle.Fill;
        splitMain.Orientation = Orientation.Vertical;
        splitMain.Size = new Size(1100, 560);
        splitMain.Panel1MinSize = 550;
        splitMain.Panel2MinSize = 310;
        splitMain.SplitterDistance = 760;
        splitMain.Padding = new Padding(8);

        // pbPreview
        pbPreview.Dock = DockStyle.Fill;
        pbPreview.BackColor = Color.FromArgb(18, 20, 28);
        pbPreview.SizeMode = PictureBoxSizeMode.Zoom;
        pbPreview.BorderStyle = BorderStyle.FixedSingle;
        splitMain.Panel1.Controls.Add(pbPreview);

        // pnlRight
        pnlRight.Dock = DockStyle.Fill;
        pnlRight.BackColor = Color.FromArgb(28, 30, 42);

        grpTrain.Text = "Yerel Eğitim / Örnek Ekleme";
        grpTrain.ForeColor = Color.LightGray;
        grpTrain.Dock = DockStyle.Top;
        grpTrain.Height = 250;
        grpTrain.Padding = new Padding(8);

        lblLabel.Text = "Kişi Etiketi:";
        lblLabel.ForeColor = Color.LightGray;
        lblLabel.Location = new Point(10, 26);
        lblLabel.AutoSize = true;
        grpTrain.Controls.Add(lblLabel);

        txtLabel.Location = new Point(92, 22);
        txtLabel.Width = 200;
        txtLabel.BackColor = Color.FromArgb(50, 54, 70);
        txtLabel.ForeColor = Color.White;
        grpTrain.Controls.Add(txtLabel);

        btnAddSample.Text = "Algılanan Yüzü Örnek Ekle";
        btnAddSample.Location = new Point(10, 56);
        btnAddSample.Size = new Size(282, 34);
        btnAddSample.BackColor = Color.FromArgb(200, 140, 30);
        btnAddSample.ForeColor = Color.White;
        btnAddSample.FlatStyle = FlatStyle.Flat;
        btnAddSample.Click += btnAddSample_Click;
        grpTrain.Controls.Add(btnAddSample);

        lstClasses.Location = new Point(10, 98);
        lstClasses.Size = new Size(282, 138);
        lstClasses.BackColor = Color.FromArgb(22, 24, 34);
        lstClasses.ForeColor = Color.LightCyan;
        lstClasses.BorderStyle = BorderStyle.None;
        lstClasses.Font = new Font("Consolas", 9.5f);
        grpTrain.Controls.Add(lstClasses);

        grpResults.Text = "Algılanan Yüzler & Güven Skoru";
        grpResults.ForeColor = Color.LightGray;
        grpResults.Dock = DockStyle.Fill;
        grpResults.Padding = new Padding(8);

        lstDetections.Dock = DockStyle.Fill;
        lstDetections.BackColor = Color.FromArgb(22, 24, 34);
        lstDetections.ForeColor = Color.LightGreen;
        lstDetections.BorderStyle = BorderStyle.None;
        lstDetections.Font = new Font("Consolas", 9.5f);
        grpResults.Controls.Add(lstDetections);

        pnlRight.Controls.Add(grpResults);
        pnlRight.Controls.Add(grpTrain);
        splitMain.Panel2.Controls.Add(pnlRight);

        // statusStrip
        statusStrip.BackColor = Color.FromArgb(32, 35, 48);
        tsslModel.Text = "Model: Beklemede";
        tsslModel.ForeColor = Color.LightGray;
        tsslFaceCount.Text = "Algılanan Yüz: 0";
        tsslFaceCount.ForeColor = Color.LightGray;
        tsslClassCount.Text = "Kayıtlı Sınıf: 0";
        tsslClassCount.ForeColor = Color.LightGray;
        statusStrip.Items.Add(tsslModel);
        statusStrip.Items.Add(new ToolStripSeparator());
        statusStrip.Items.Add(tsslFaceCount);
        statusStrip.Items.Add(new ToolStripSeparator());
        statusStrip.Items.Add(tsslClassCount);

        // MainForm
        this.Text = "YMH 219 - OpenCV Yüz Algılama ve Tanıma";
        this.Size = new Size(1120, 680);
        this.MinimumSize = new Size(960, 580);
        this.BackColor = Color.FromArgb(24, 26, 36);
        this.Controls.Add(splitMain);
        this.Controls.Add(pnlTop);
        this.Controls.Add(statusStrip);
        this.AutoScaleMode = AutoScaleMode.Font;
        this.Load += MainForm_Load;
        this.FormClosing += MainForm_FormClosing;
    }

    private Panel pnlTop, pnlRight;
    private Button btnStartCam, btnStopCam, btnLoadImage, btnSaveFaces, btnAddSample;
    private Label lblStatus, lblLabel;
    private TextBox txtLabel;
    private SplitContainer splitMain;
    private PictureBox pbPreview;
    private GroupBox grpTrain, grpResults;
    private ListBox lstClasses, lstDetections;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel tsslModel, tsslFaceCount, tsslClassCount;

    #endregion
}
