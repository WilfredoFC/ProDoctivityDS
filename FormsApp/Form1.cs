using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

// PdfPig – solo para leer texto (análisis)
using PdfPigDoc = UglyToad.PdfPig.PdfDocument;

// PDFsharp – para modificar PDF (eliminar páginas)
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharpDoc = PdfSharp.Pdf.PdfDocument;

namespace ProdoctivityPdfProcessor
{
    public partial class Form1 : Form
    {
        // ---------- API Configuration ----------
        private string BaseUrl = "https://cloud.prodoctivity.com/api/";
        private string ApiKey = "pdoca42e8b24bab91a92e0ec9037fc58e25f";
        private string ApiSecret = "35bb5e5194e096af1d4881d5198d5f9fcfdb18b6dcaa869a223129de66f76d85";
        private string BearerToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpYXQiOjE3NzA4MjM0OTIuMjUzLCJwZXJtaXNzaW9ucyI6WyJkb2N1bWVudC1jb2xsZWN0aW9uLXRhc2siLCJnZW5lcmF0aW9uLW1vbml0b3IiLCJwdWJsaXNoLXRlbXBsYXRlIiwic2hhcmUtZG9jdW1lbnQtbW9uaXRvciIsImNhbi1pbXBlcnNvbmF0ZSIsImRlbGV0ZS1kb2N1bWVudC1jb2xsZWN0aW9uIiwiZGVsZXRlLWRvY3VtZW50IiwidGFza3MtbWFuYWdlciIsImRlbGV0ZS10ZW1wbGF0ZSIsIm9yZ2FuaXphdGlvbi1hZG1pbiJdLCJvcmdhbml6YXRpb25JZCI6ImJhcm5hZG8iLCJtZmFBdXRoZW50aWNhdGVkIjpmYWxzZSwidXNlcm5hbWUiOiJXZmVsaXpAbm92b3NpdC5jb20iLCJqdGkiOiJhNDI5ZTVmNi1iM2JjLTQyZDctOTEwNy03MGU2MWFhMzJiY2QiLCJuYmYiOjE3NzA4MjM0OTIsImV4cCI6MTc3MDgyNTI5MCwiYXVkIjoiYXBwLXVzZXIiLCJpc3MiOiJQcm9Eb2N0aXZpdHkiLCJzdWIiOiJXZmVsaXpAbm92b3NpdC5jb20ifQ.4803xQCkL8oz8aiA-J2q26hViXFq7syK0OnSAESJwUs";
        private string Cookie = "PRODOC-SESSIONID=cf7f4b726ee5c61361a12a8e81e55bba|28133ace3d4d3e232a9164fcaf7f411e";

        // ---------- Search Criteria ----------
        private string SearchDocumentTypeIds = "";
        private string SearchName = "";
        private int SearchPageNumber = 0;
        private int SearchRowsPerPage = 100;

        // ---------- Processing Options ----------
        private bool AnalyzeFirstPage = true;
        private bool RemoveFirstPage = true;
        private bool UpdateInApi = true;
        private bool SaveOriginals = false;
        private bool CreateBackup = true;

        // ---------- Analysis Keywords ----------
        private string KeywordSeparador = "SEPARADOR DE DOCUMENTOS";
        private string KeywordCodigo = "DOC-001";

        // ---------- Normalisation Options ----------
        private bool NormalizeText = true;
        private bool RemoveAccents = true;
        private bool RemovePunctuation = true;
        private bool RemoveExtraSpaces = true;
        private bool IgnoreLineBreaks = true;
        private bool CaseSensitive = false;
        private bool UseRegex = false;

        // ---------- Data Storage ----------
        private List<JObject> FoundDocuments = new List<JObject>();
        private Dictionary<string, bool> SelectedDocuments = new Dictionary<string, bool>();
        private Dictionary<string, string> DocumentTypeIds = new Dictionary<string, string>();
        private Dictionary<string, string> DocumentVersionIds = new Dictionary<string, string>();

        // ---------- UI Controls ----------
        private TabControl tabControl;
        private DataGridView dgvResults;
        private TextBox txtBaseUrl, txtApiKey, txtApiSecret, txtBearerToken, txtCookie;
        private TextBox txtSearchTypeIds, txtSearchName;
        private NumericUpDown numPageNumber, numRowsPerPage;
        private Label lblSearchCount, lblSelectedCount;
        private Button btnSearch, btnClearSearch, btnCopyTypeIds;
        private Button btnSelectAll, btnDeselectAll, btnInvertSelection;
        private CheckBox chkAnalyzeFirstPage, chkRemoveFirstPage, chkUpdateInApi, chkSaveOriginals, chkCreateBackup;
        private TextBox txtKeywordSeparador, txtKeywordCodigo;
        private CheckBox chkNormalizeText, chkRemoveAccents, chkRemovePunctuation, chkRemoveExtraSpaces, chkIgnoreLineBreaks, chkCaseSensitive, chkUseRegex;
        private Button btnTestAnalysis, btnShowNormalizationExample;
        private Label lblTestResult;
        private RichTextBox txtLog;
        private ProgressBar progressBar;
        private Label lblProcessed, lblUpdated, lblRemoved, lblErrors, lblSkipped, lblStatus;
        private Button btnProcess, btnStop, btnOpenFolder;

        private CancellationTokenSource cancellationTokenSource;
        private bool processing = false;

        public Form1()
        {
            InitializeComponent();
            this.Load += async (s, e) => await SearchDocumentsAsync();
        }

        private void InitializeComponent()
        {
            this.Text = "Procesador Inteligente de Documentos PDF - Prodoctivity";
            this.Size = new Size(1400, 900);
            this.MinimumSize = new Size(1200, 700);
            this.StartPosition = FormStartPosition.CenterScreen;

            tabControl = new TabControl { Dock = DockStyle.Fill };
            this.Controls.Add(tabControl);

            CreateSearchTab();
            CreateProcessingTab();
            CreateAnalysisTab();
            CreateLogTab();

            CreateMenu();
        }

        // ---------- UI Creation ----------
        private void CreateMenu()
        {
            var menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Archivo");
            fileMenu.DropDownItems.Add("Guardar configuración", null, SaveConfig);
            fileMenu.DropDownItems.Add("Cargar configuración", null, LoadConfig);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add("Salir", null, (s, e) => Close());
            menuStrip.Items.Add(fileMenu);

            var toolsMenu = new ToolStripMenuItem("Herramientas");
            toolsMenu.DropDownItems.Add("Verificar conexión API", null, TestConnection);
            toolsMenu.DropDownItems.Add("Acerca de PDFsharp", null, (s, e) => MessageBox.Show("Usando PDFsharp para manipulación de PDF.", "Información"));
            menuStrip.Items.Add(toolsMenu);

            var helpMenu = new ToolStripMenuItem("Ayuda");
            helpMenu.DropDownItems.Add("Documentación", null, (s, e) => System.Diagnostics.Process.Start("https://cloudx.prodoctivity.com/docs/"));
            helpMenu.DropDownItems.Add("Acerca de", null, ShowAbout);
            menuStrip.Items.Add(helpMenu);

            this.MainMenuStrip = menuStrip;
            this.Controls.Add(menuStrip);
        }

        // ==================== PESTAÑA DE BÚSQUEDA ====================
        private void CreateSearchTab()
        {
            var tab = new TabPage("🔍 Buscar Documentos");
            tab.AutoScroll = true;

            // TableLayout con 2 filas: criterios (alto fijo) y resultados (resto)
            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 2,
                Padding = new Padding(5),
                AutoSize = false
            };
            // Fila 0: altura fija de 180px
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 180));
            // Fila 1: ocupa el 100% del espacio restante
            tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            tab.Controls.Add(tableLayout);

            // ----- Panel de criterios (fila 0) -----
            var criteriaPanel = new GroupBox
            {
                Text = "Criterios de Búsqueda",
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            var criteriaLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(10),
                AutoSize = false
            };
            criteriaPanel.Controls.Add(criteriaLayout);

            // Fila 0: Document Type IDs
            criteriaLayout.Controls.Add(new Label { Text = "Filtrar por Document Type IDs:", Anchor = AnchorStyles.Left }, 0, 0);
            txtSearchTypeIds = new TextBox { Width = 400, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            criteriaLayout.Controls.Add(txtSearchTypeIds, 1, 0);
            criteriaLayout.Controls.Add(new Label { Text = "(dejar vacío para ver TODOS)", Anchor = AnchorStyles.Left }, 2, 0);

            // Fila 1: Nombre
            criteriaLayout.Controls.Add(new Label { Text = "Filtrar por nombre:", Anchor = AnchorStyles.Left }, 0, 1);
            txtSearchName = new TextBox { Width = 400, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            criteriaLayout.Controls.Add(txtSearchName, 1, 1);
            criteriaLayout.Controls.Add(new Label { Text = "(buscar por nombre, opcional)", Anchor = AnchorStyles.Left }, 2, 1);

            // Fila 2: Página y resultados por página
            criteriaLayout.Controls.Add(new Label { Text = "Página:", Anchor = AnchorStyles.Left }, 0, 2);
            numPageNumber = new NumericUpDown { Minimum = 0, Maximum = 1000, Value = 0, Width = 80 };
            criteriaLayout.Controls.Add(numPageNumber, 1, 2);
            criteriaLayout.Controls.Add(new Label { Text = "Resultados por página:", Anchor = AnchorStyles.Left }, 2, 2);
            numRowsPerPage = new NumericUpDown { Minimum = 1, Maximum = 500, Value = 100, Width = 80 };
            criteriaLayout.Controls.Add(numRowsPerPage, 3, 2);

            // Fila 3: Botones
            btnSearch = new Button { Text = "🔍 Buscar Documentos", Width = 180 };
            btnSearch.Click += async (s, e) => await SearchDocumentsAsync();
            criteriaLayout.Controls.Add(btnSearch, 0, 3);

            btnClearSearch = new Button { Text = "🗑️ Limpiar Búsqueda", Width = 150 };
            btnClearSearch.Click += (s, e) => ClearSearch();
            criteriaLayout.Controls.Add(btnClearSearch, 1, 3);

            btnCopyTypeIds = new Button { Text = "📋 Copiar Type IDs Seleccionados", Width = 220 };
            btnCopyTypeIds.Click += CopySelectedTypeIds;
            criteriaLayout.Controls.Add(btnCopyTypeIds, 2, 3);

            lblSearchCount = new Label
            {
                Text = "0 documentos encontrados",
                Font = new Font("Arial", 9, FontStyle.Bold),
                Anchor = AnchorStyles.Right
            };
            criteriaLayout.Controls.Add(lblSearchCount, 3, 3);

            tableLayout.Controls.Add(criteriaPanel, 0, 0);

            // ----- Panel de resultados (fila 1) -----
            var resultsPanel = new GroupBox { Text = "Resultados de Búsqueda", Dock = DockStyle.Fill };
            var resultsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
            resultsPanel.Controls.Add(resultsLayout);

            dgvResults = new DataGridView
            {
                Dock = DockStyle.Fill,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = false,
                RowHeadersVisible = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            dgvResults.CellContentClick += DgvResults_CellContentClick;
            resultsLayout.Controls.Add(dgvResults, 0, 0);

            var selectionPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 5, 0, 0)
            };
            btnSelectAll = new Button { Text = "☑️ Seleccionar Todos", Width = 150 };
            btnSelectAll.Click += (s, e) => SelectAll();
            selectionPanel.Controls.Add(btnSelectAll);

            btnDeselectAll = new Button { Text = "☐ Deseleccionar Todos", Width = 150 };
            btnDeselectAll.Click += (s, e) => DeselectAll();
            selectionPanel.Controls.Add(btnDeselectAll);

            btnInvertSelection = new Button { Text = "🔄 Invertir Selección", Width = 150 };
            btnInvertSelection.Click += (s, e) => InvertSelection();
            selectionPanel.Controls.Add(btnInvertSelection);

            lblSelectedCount = new Label
            {
                Text = "0 seleccionados",
                Font = new Font("Arial", 9, FontStyle.Bold),
                ForeColor = Color.DodgerBlue,
                AutoSize = true,
                Margin = new Padding(20, 0, 0, 0)
            };
            selectionPanel.Controls.Add(lblSelectedCount);

            resultsLayout.Controls.Add(selectionPanel, 0, 1);

            tableLayout.Controls.Add(resultsPanel, 0, 1);

            tabControl.TabPages.Add(tab);
        }

        // ==================== PESTAÑA DE PROCESAMIENTO ====================
        private void CreateProcessingTab()
        {
            var tab = new TabPage("⚙️ Procesar Documentos");
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(5),
                AutoSize = false
            };
            // Fila 0: API config → autoajuste
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Fila 1: Opciones → autoajuste
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            // Fila 2: Estado → ocupa todo el espacio restante (100%)
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            // Fila 3: Botones → autoajuste
            layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tab.Controls.Add(layout);

            // ----- Configuración API -----
            var apiGroup = new GroupBox
            {
                Text = "🔧 Configuración API",
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            var apiLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 5,
                Padding = new Padding(10),
                AutoSize = false
            };
            apiGroup.Controls.Add(apiLayout);

            apiLayout.Controls.Add(new Label { Text = "Base URL:", Anchor = AnchorStyles.Left }, 0, 0);
            txtBaseUrl = new TextBox { Text = BaseUrl, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            apiLayout.Controls.Add(txtBaseUrl, 1, 0);
            apiLayout.SetColumnSpan(txtBaseUrl, 2);

            apiLayout.Controls.Add(new Label { Text = "API Key:", Anchor = AnchorStyles.Left }, 0, 1);
            txtApiKey = new TextBox { Text = ApiKey, Anchor = AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
            apiLayout.Controls.Add(txtApiKey, 1, 1);
            var btnShowApiKey = new Button { Text = "Mostrar", Width = 80 };
            btnShowApiKey.Click += (s, e) => ToggleShow(txtApiKey);
            apiLayout.Controls.Add(btnShowApiKey, 2, 1);

            apiLayout.Controls.Add(new Label { Text = "API Secret:", Anchor = AnchorStyles.Left }, 0, 2);
            txtApiSecret = new TextBox { Text = ApiSecret, Anchor = AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
            apiLayout.Controls.Add(txtApiSecret, 1, 2);
            var btnShowSecret = new Button { Text = "Mostrar", Width = 80 };
            btnShowSecret.Click += (s, e) => ToggleShow(txtApiSecret);
            apiLayout.Controls.Add(btnShowSecret, 2, 2);

            apiLayout.Controls.Add(new Label { Text = "Bearer Token:", Anchor = AnchorStyles.Left }, 0, 3);
            txtBearerToken = new TextBox { Text = BearerToken, Anchor = AnchorStyles.Left | AnchorStyles.Right, UseSystemPasswordChar = true };
            apiLayout.Controls.Add(txtBearerToken, 1, 3);
            var btnShowToken = new Button { Text = "Mostrar", Width = 80 };
            btnShowToken.Click += (s, e) => ToggleShow(txtBearerToken);
            apiLayout.Controls.Add(btnShowToken, 2, 3);

            apiLayout.Controls.Add(new Label { Text = "Cookie:", Anchor = AnchorStyles.Left }, 0, 4);
            txtCookie = new TextBox { Text = Cookie, Anchor = AnchorStyles.Left | AnchorStyles.Right };
            apiLayout.Controls.Add(txtCookie, 1, 4);
            apiLayout.SetColumnSpan(txtCookie, 2);

            layout.Controls.Add(apiGroup, 0, 0);

            // ----- Opciones de Procesamiento -----
            var optionsGroup = new GroupBox
            {
                Text = "⚙️ Opciones de Procesamiento",
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            var optionsLayout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(10),
                WrapContents = false,
                AutoSize = false
            };
            optionsGroup.Controls.Add(optionsLayout);

            chkRemoveFirstPage = new CheckBox { Text = "Remover primera página de PDFs", Checked = RemoveFirstPage, AutoSize = true };
            optionsLayout.Controls.Add(chkRemoveFirstPage);
            chkAnalyzeFirstPage = new CheckBox { Text = "Solo si cumple criterios de análisis", Checked = AnalyzeFirstPage, AutoSize = true };
            optionsLayout.Controls.Add(chkAnalyzeFirstPage);
            chkUpdateInApi = new CheckBox { Text = "Actualizar documentos en la API", Checked = UpdateInApi, AutoSize = true };
            optionsLayout.Controls.Add(chkUpdateInApi);
            chkSaveOriginals = new CheckBox { Text = "Guardar originales", Checked = SaveOriginals, AutoSize = true };
            optionsLayout.Controls.Add(chkSaveOriginals);
            chkCreateBackup = new CheckBox { Text = "Crear backup de datos", Checked = CreateBackup, AutoSize = true };
            optionsLayout.Controls.Add(chkCreateBackup);

            layout.Controls.Add(optionsGroup, 0, 1);

            // ----- Estado del Proceso -----
            var statusGroup = new GroupBox
            {
                Text = "📊 Estado del Proceso",
                Dock = DockStyle.Fill,
                AutoSize = false
            };
            var statusLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 10,
                RowCount = 3,
                Padding = new Padding(10),
                AutoSize = false
            };
            statusGroup.Controls.Add(statusLayout);

            lblProcessed = new Label { Text = "0", ForeColor = Color.Green, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            lblUpdated = new Label { Text = "0", ForeColor = Color.DodgerBlue, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            lblRemoved = new Label { Text = "0", ForeColor = Color.Purple, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            lblErrors = new Label { Text = "0", ForeColor = Color.Red, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };
            lblSkipped = new Label { Text = "0", ForeColor = Color.Gray, Font = new Font("Arial", 10, FontStyle.Bold), AutoSize = true };

            statusLayout.Controls.Add(new Label { Text = "Procesados:", AutoSize = true }, 0, 0);
            statusLayout.Controls.Add(lblProcessed, 1, 0);
            statusLayout.Controls.Add(new Label { Text = "Actualizados:", AutoSize = true }, 2, 0);
            statusLayout.Controls.Add(lblUpdated, 3, 0);
            statusLayout.Controls.Add(new Label { Text = "Páginas removidas:", AutoSize = true }, 4, 0);
            statusLayout.Controls.Add(lblRemoved, 5, 0);
            statusLayout.Controls.Add(new Label { Text = "Errores:", AutoSize = true }, 6, 0);
            statusLayout.Controls.Add(lblErrors, 7, 0);
            statusLayout.Controls.Add(new Label { Text = "Saltados:", AutoSize = true }, 8, 0);
            statusLayout.Controls.Add(lblSkipped, 9, 0);

            progressBar = new ProgressBar { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100, Value = 0 };
            statusLayout.Controls.Add(progressBar, 0, 1);
            statusLayout.SetColumnSpan(progressBar, 10);

            lblStatus = new Label { Text = "Listo para comenzar", AutoSize = true };
            statusLayout.Controls.Add(lblStatus, 0, 2);
            statusLayout.SetColumnSpan(lblStatus, 10);

            layout.Controls.Add(statusGroup, 0, 2);

            // ----- Botones de control -----
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                Padding = new Padding(0, 10, 0, 0),
                AutoSize = false
            };
            btnProcess = new Button { Text = "🗑️ Eliminar Seleccionados", Width = 200 };
            btnProcess.Click += ProcessSelectedOnly;
            buttonPanel.Controls.Add(btnProcess);

            btnStop = new Button { Text = "⏹️ Detener", Width = 150, Enabled = false };
            btnStop.Click += StopProcessing;
            buttonPanel.Controls.Add(btnStop);

            btnOpenFolder = new Button { Text = "📂 Abrir Carpeta", Width = 150 };
            btnOpenFolder.Click += OpenOutputFolder;
            buttonPanel.Controls.Add(btnOpenFolder);

            layout.Controls.Add(buttonPanel, 0, 3);

            tabControl.TabPages.Add(tab);
        }

        // ==================== PESTAÑA DE ANÁLISIS ====================
        private void CreateAnalysisTab()
        {
            var tab = new TabPage("🔍 Análisis de Páginas");
            var layout = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(10),
                AutoScroll = true,
                AutoSize = false
            };
            tab.Controls.Add(layout);

            // ----- Criterios de análisis -----
            var criteriaGroup = new GroupBox
            {
                Text = "🔎 Criterios para Remover Primera Página",
                AutoSize = false,
                Width = 900,
                Height = 400
            };
            var criteriaLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 6,
                Padding = new Padding(10),
                AutoSize = false
            };
            criteriaGroup.Controls.Add(criteriaLayout);

            criteriaLayout.Controls.Add(new Label { Text = "La primera página será removida SOLO si contiene:", Font = new Font("Arial", 10, FontStyle.Bold) }, 0, 0);
            criteriaLayout.SetColumnSpan(criteriaLayout.GetControlFromPosition(0, 0), 3);

            // 1. Texto separador
            criteriaLayout.Controls.Add(new Label { Text = "1. Texto 'SEPARADOR DE DOCUMENTOS':", Font = new Font("Arial", 9) }, 0, 1);
            txtKeywordSeparador = new TextBox { Text = KeywordSeparador, Width = 300 };
            criteriaLayout.Controls.Add(txtKeywordSeparador, 1, 1);
            criteriaLayout.Controls.Add(new Label { Text = "(buscará variantes como 'SEPARADOR DOCUMENTOS', 'HOJA SEPARADORA', etc.)" }, 2, 1);

            // 2. Código de documento
            criteriaLayout.Controls.Add(new Label { Text = "2. Código de documento (patrón):", Font = new Font("Arial", 9) }, 0, 2);
            txtKeywordCodigo = new TextBox { Text = KeywordCodigo, Width = 300 };
            criteriaLayout.Controls.Add(txtKeywordCodigo, 1, 2);
            criteriaLayout.Controls.Add(new Label { Text = "(ej: DOC-001, DOC001, DOC 001, DOC - 001, etc.)" }, 2, 2);

            // ----- Normalización -----
            var normGroup = new GroupBox { Text = "⚙️ Normalización de Texto para Análisis", AutoSize = true, Dock = DockStyle.Fill };
            var normLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 4, Padding = new Padding(10), AutoSize = true };
            normGroup.Controls.Add(normLayout);

            chkNormalizeText = new CheckBox { Text = "Normalizar texto para análisis", Checked = NormalizeText, AutoSize = true };
            normLayout.Controls.Add(chkNormalizeText, 0, 0);
            normLayout.SetColumnSpan(chkNormalizeText, 2);

            chkRemoveAccents = new CheckBox { Text = "Remover acentos (á → a, é → e, etc.)", Checked = RemoveAccents, AutoSize = true };
            normLayout.Controls.Add(chkRemoveAccents, 0, 1);
            chkRemovePunctuation = new CheckBox { Text = "Remover puntuación", Checked = RemovePunctuation, AutoSize = true };
            normLayout.Controls.Add(chkRemovePunctuation, 1, 1);

            chkRemoveExtraSpaces = new CheckBox { Text = "Remover espacios extras", Checked = RemoveExtraSpaces, AutoSize = true };
            normLayout.Controls.Add(chkRemoveExtraSpaces, 0, 2);
            chkIgnoreLineBreaks = new CheckBox { Text = "Ignorar saltos de línea", Checked = IgnoreLineBreaks, AutoSize = true };
            normLayout.Controls.Add(chkIgnoreLineBreaks, 1, 2);

            chkCaseSensitive = new CheckBox { Text = "Búsqueda sensible a mayúsculas", Checked = CaseSensitive, AutoSize = true };
            normLayout.Controls.Add(chkCaseSensitive, 0, 3);
            chkUseRegex = new CheckBox { Text = "Usar expresiones regulares", Checked = UseRegex, AutoSize = true };
            normLayout.Controls.Add(chkUseRegex, 1, 3);

            criteriaLayout.Controls.Add(normGroup, 0, 3);
            criteriaLayout.SetColumnSpan(normGroup, 3);

            // Botones de prueba
            var testPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, AutoSize = true, Margin = new Padding(0, 10, 0, 0) };
            btnTestAnalysis = new Button { Text = "🧪 Probar Análisis en PDF", Width = 180 };
            btnTestAnalysis.Click += TestAnalysis;
            testPanel.Controls.Add(btnTestAnalysis);
            btnShowNormalizationExample = new Button { Text = "📄 Ver Ejemplo de Normalización", Width = 220 };
            btnShowNormalizationExample.Click += ShowNormalizationExample;
            testPanel.Controls.Add(btnShowNormalizationExample);
            criteriaLayout.Controls.Add(testPanel, 0, 4);
            criteriaLayout.SetColumnSpan(testPanel, 3);

            lblTestResult = new Label { Text = "", AutoSize = true, ForeColor = Color.Purple, MaximumSize = new Size(800, 0) };
            criteriaLayout.Controls.Add(lblTestResult, 0, 5);
            criteriaLayout.SetColumnSpan(lblTestResult, 3);

            layout.Controls.Add(criteriaGroup);

            // ----- Información adicional -----
            var infoGroup = new GroupBox
            {
                Text = "ℹ️ Información sobre la Normalización de Texto",
                AutoSize = false,
                Width = 900,
                Height = 300
            };
            var infoText = new Label
            {
                Text = @"La normalización de texto mejora la detección de patrones al:

1. Convertir todo a MAYÚSCULAS para búsqueda case-insensitive
2. Remover acentos: ""SEPARADOR"" = ""SEPARADOR"" o ""SEPARADOR""
3. Remover puntuación: ""DOC-001"" = ""DOC 001""
4. Unir líneas: texto con saltos se convierte en una sola línea
5. Eliminar espacios extras: ""DOC  001"" = ""DOC 001""

Ejemplos de normalización:
• ""Sépárádör de Dócüméntös"" → ""SEPARADOR DE DOCUMENTOS""
• ""DOC - 001\nPágina 1"" → ""DOC 001 PAGINA 1""
• ""Hoja   separadora  de  documentos"" → ""HOJA SEPARADORA DE DOCUMENTOS""

Esto permite detectar patrones incluso cuando el formato varía.",
                AutoSize = true,
                MaximumSize = new Size(880, 0),
                Padding = new Padding(10)
            };
            infoGroup.Controls.Add(infoText);
            layout.Controls.Add(infoGroup);

            tabControl.TabPages.Add(tab);
        }

        // ==================== PESTAÑA DE LOG ====================
        private void CreateLogTab()
        {
            var tab = new TabPage("📝 Log de Actividades");
            var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(5) };
            tab.Controls.Add(layout);

            txtLog = new RichTextBox { Dock = DockStyle.Fill, Font = new Font("Consolas", 9), ReadOnly = true, BackColor = Color.White };
            layout.Controls.Add(txtLog, 0, 0);

            var buttonPanel = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight };
            var btnClearLog = new Button { Text = "🧹 Limpiar Log", Width = 120 };
            btnClearLog.Click += (s, e) => txtLog.Clear();
            buttonPanel.Controls.Add(btnClearLog);

            var btnSaveLog = new Button { Text = "💾 Guardar Log", Width = 120 };
            btnSaveLog.Click += SaveLog;
            buttonPanel.Controls.Add(btnSaveLog);

            var btnCopyLog = new Button { Text = "📋 Copiar Log", Width = 120 };
            btnCopyLog.Click += (s, e) => { Clipboard.SetText(txtLog.Text); Log("📋 Log copiado al portapapeles", "success"); };
            buttonPanel.Controls.Add(btnCopyLog);

            layout.Controls.Add(buttonPanel, 0, 1);

            tabControl.TabPages.Add(tab);
        }

        // ---------- Helpers ----------
        private void ToggleShow(TextBox tb) => tb.UseSystemPasswordChar = !tb.UseSystemPasswordChar;

        // ---------- Logging ----------
        private void Log(string message, string level = "info")
        {
            string timestamp = DateTime.Now.ToString("HH:mm:ss");
            Color color = level switch
            {
                "success" => Color.Green,
                "warning" => Color.Orange,
                "error" => Color.Red,
                "system" => Color.Blue,
                "analysis" => Color.Purple,
                "skipped" => Color.Gray,
                _ => Color.Black
            };
            txtLog.Invoke(new Action(() =>
            {
                txtLog.SelectionStart = txtLog.TextLength;
                txtLog.SelectionLength = 0;
                txtLog.SelectionColor = color;
                txtLog.AppendText($"[{timestamp}] {message}\n");
                txtLog.SelectionColor = txtLog.ForeColor;
                txtLog.ScrollToCaret();
            }));
        }

        private void SaveLog(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog { Filter = "Log files|*.log|Text files|*.txt|All files|*.*", DefaultExt = "log" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, txtLog.Text);
                Log($"✅ Log guardado en: {sfd.FileName}", "success");
            }
        }

        // ---------- Search ----------
        private async Task SearchDocumentsAsync()
        {
            Log("🔍 Buscando documentos...", "system");

            try
            {
                SearchDocumentTypeIds = txtSearchTypeIds.Text;
                SearchName = txtSearchName.Text;
                SearchPageNumber = (int)numPageNumber.Value;
                SearchRowsPerPage = (int)numRowsPerPage.Value;

                string url = $"{BaseUrl}app/documents?dateStart=0&pageNumber={SearchPageNumber}&rowsPerPage={SearchRowsPerPage}&sortField=updatedAt&sortDirection=DESC";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                client.DefaultRequestHeaders.Add("x-api-secret", ApiSecret);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
                client.DefaultRequestHeaders.Add("Cookie", Cookie);

                HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Get, url);
                if (!string.IsNullOrWhiteSpace(SearchDocumentTypeIds))
                {
                    var typeIds = SearchDocumentTypeIds.Split(',').Select(id => id.Trim()).Where(id => !string.IsNullOrEmpty(id)).ToList();
                    var payload = new Dictionary<string, object> { { "documentTypeIdList[]", typeIds } };
                    string jsonPayload = JsonConvert.SerializeObject(payload);
                    request.Content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                    Log($"Filtrando por tipos en body: {string.Join(", ", typeIds)}", "info");
                }

                HttpResponseMessage response = await client.SendAsync(request);
                Log($"Status Code: {(int)response.StatusCode}", "system");

                if (!response.IsSuccessStatusCode)
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Log($"❌ Error en la búsqueda: {response.StatusCode}", "error");
                    Log($"Respuesta: {error[..Math.Min(500, error.Length)]}", "error");
                    return;
                }

                string json = await response.Content.ReadAsStringAsync();
                var data = JObject.Parse(json);

                Log($"✅ Respuesta exitosa recibida", "success");

                FoundDocuments.Clear();
                SelectedDocuments.Clear();
                DocumentTypeIds.Clear();
                DocumentVersionIds.Clear();

                if (data["documents"] is JArray docsArray)
                {
                    foreach (var doc in docsArray)
                        FoundDocuments.Add((JObject)doc);
                    Log($"✅ Encontrados {FoundDocuments.Count} documentos en la clave 'documents'", "success");
                }
                else
                    Log("⚠️ No se encontró la clave 'documents' en la respuesta", "warning");

                // DataGridView setup
                dgvResults.Columns.Clear();
                dgvResults.Columns.Add(new DataGridViewCheckBoxColumn { Name = "selected", HeaderText = "✓", Width = 50 });
                dgvResults.Columns.Add("id", "Document ID");
                dgvResults.Columns.Add("name", "Nombre");
                dgvResults.Columns.Add("type", "Tipo");
                dgvResults.Columns.Add("type_id", "Type ID");
                dgvResults.Columns.Add("created", "Creado");
                dgvResults.Columns.Add("pages", "Páginas");
                dgvResults.Columns.Add("analysis", "Análisis 1ra Pág");

                dgvResults.Rows.Clear();

                foreach (var doc in FoundDocuments)
                {
                    string docId = doc["documentId"]?.ToString() ?? doc["id"]?.ToString() ?? "";
                    string name = doc["name"]?.ToString() ?? "Sin nombre";
                    string type = doc["documentTypeName"]?.ToString() ?? doc["type"]?.ToString() ?? "Sin tipo";
                    string typeId = doc["documentTypeId"]?.ToString() ?? doc["typeId"]?.ToString() ?? "";
                    string versionId = doc["lastDocumentVersionId"]?.ToString() ?? doc["documentVersionId"]?.ToString() ?? "";
                    string createdAt = doc["createdAt"]?.ToString() ?? "";

                    if (!string.IsNullOrEmpty(docId))
                    {
                        DocumentTypeIds[docId] = typeId;
                        if (!string.IsNullOrEmpty(versionId))
                            DocumentVersionIds[docId] = versionId;
                    }

                    string displayDate = "N/A";
                    if (long.TryParse(createdAt, out long ts))
                    {
                        if (ts > 1000000000000) ts /= 1000;
                        displayDate = DateTimeOffset.FromUnixTimeSeconds(ts).DateTime.ToString("yyyy-MM-dd");
                    }

                    int rowIdx = dgvResults.Rows.Add(false, docId, name, type, typeId, displayDate, "?", "Pendiente");
                    dgvResults.Rows[rowIdx].Tag = docId;
                    SelectedDocuments[docId] = false;
                }

                lblSearchCount.Text = $"{FoundDocuments.Count} documentos encontrados";
                UpdateSelectionCount();
            }
            catch (Exception ex)
            {
                Log($"❌ Error en búsqueda: {ex.Message}", "error");
            }
        }

        private void DgvResults_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex == 0)
            {
                var cell = dgvResults.Rows[e.RowIndex].Cells[0] as DataGridViewCheckBoxCell;
                bool newVal = !(bool)(cell.Value ?? false);
                cell.Value = newVal;
                string docId = dgvResults.Rows[e.RowIndex].Tag?.ToString();
                if (docId != null && SelectedDocuments.ContainsKey(docId))
                    SelectedDocuments[docId] = newVal;
                UpdateSelectionCount();
            }
        }

        private void UpdateSelectionCount()
        {
            int count = SelectedDocuments.Count(kv => kv.Value);
            lblSelectedCount.Text = $"{count} seleccionados";
        }

        private void SelectAll()
        {
            foreach (DataGridViewRow row in dgvResults.Rows)
            {
                row.Cells[0].Value = true;
                string docId = row.Tag?.ToString();
                if (docId != null) SelectedDocuments[docId] = true;
            }
            UpdateSelectionCount();
            Log("✅ Todos los documentos seleccionados", "success");
        }

        private void DeselectAll()
        {
            foreach (DataGridViewRow row in dgvResults.Rows)
            {
                row.Cells[0].Value = false;
                string docId = row.Tag?.ToString();
                if (docId != null) SelectedDocuments[docId] = false;
            }
            UpdateSelectionCount();
            Log("✅ Todos los documentos deseleccionados", "success");
        }

        private void InvertSelection()
        {
            foreach (DataGridViewRow row in dgvResults.Rows)
            {
                bool current = (bool)(row.Cells[0].Value ?? false);
                bool newVal = !current;
                row.Cells[0].Value = newVal;
                string docId = row.Tag?.ToString();
                if (docId != null) SelectedDocuments[docId] = newVal;
            }
            UpdateSelectionCount();
            Log("✅ Selección invertida", "success");
        }

        private void ClearSearch()
        {
            dgvResults.Rows.Clear();
            FoundDocuments.Clear();
            SelectedDocuments.Clear();
            DocumentTypeIds.Clear();
            DocumentVersionIds.Clear();
            lblSearchCount.Text = "0 documentos encontrados";
            lblSelectedCount.Text = "0 seleccionados";
            Log("🧹 Búsqueda limpiada", "system");
        }

        private void CopySelectedTypeIds(object sender, EventArgs e)
        {
            var selectedTypeIds = SelectedDocuments
                .Where(kv => kv.Value && DocumentTypeIds.ContainsKey(kv.Key))
                .Select(kv => DocumentTypeIds[kv.Key])
                .Where(id => !string.IsNullOrEmpty(id))
                .Distinct()
                .ToList();

            if (selectedTypeIds.Any())
            {
                string idsText = string.Join(Environment.NewLine, selectedTypeIds);
                Clipboard.SetText(idsText);
                Log($"📋 {selectedTypeIds.Count} Type IDs copiados al portapapeles", "success");
                MessageBox.Show($"Se copiaron {selectedTypeIds.Count} Type IDs al portapapeles.", "Type IDs Copiados");
            }
            else
            {
                MessageBox.Show("No hay documentos seleccionados con Type IDs válidos.", "Sin Type IDs");
            }
        }

        // ---------- Analysis (PdfPig) ----------
        private string NormalizeTextForAnalysis(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            string normalized = text;

            if (!CaseSensitive)
                normalized = normalized.ToUpperInvariant();

            if (NormalizeText)
            {
                if (RemoveAccents)
                {
                    normalized = normalized.Normalize(NormalizationForm.FormD);
                    var chars = normalized.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray();
                    normalized = new string(chars).Normalize(NormalizationForm.FormC);
                }

                if (IgnoreLineBreaks)
                    normalized = normalized.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");

                if (RemovePunctuation)
                    normalized = Regex.Replace(normalized, @"[^\w\s]", " ");

                if (RemoveExtraSpaces)
                    normalized = Regex.Replace(normalized, @"\s+", " ").Trim();
            }

            return normalized;
        }

        private (bool shouldRemove, string diagnosis) AnalyzeFirstPageContent(byte[] pdfBytes)
        {
            try
            {
                using var stream = new MemoryStream(pdfBytes);
                using var pdf = PdfPigDoc.Open(stream);

                UglyToad.PdfPig.Content.Page page;
                try
                {
                    page = pdf.GetPage(1);
                }
                catch
                {
                    return (false, "PDF vacío o sin páginas");
                }

                string pageText = page.Text;
                string normalizedText = NormalizeTextForAnalysis(pageText);

                string sep = KeywordSeparador;
                string cod = KeywordCodigo;
                if (NormalizeText)
                {
                    sep = NormalizeTextForAnalysis(sep);
                    cod = NormalizeTextForAnalysis(cod);
                }
                else if (!CaseSensitive)
                {
                    sep = sep.ToUpperInvariant();
                    cod = cod.ToUpperInvariant();
                }

                bool foundSeparador = false;
                bool foundCodigo = false;
                var separadorVariants = new List<string>();
                var codigoMatches = new List<string>();

                if (!string.IsNullOrEmpty(sep))
                {
                    if (UseRegex)
                    {
                        try
                        {
                            foundSeparador = Regex.IsMatch(normalizedText, sep, CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        }
                        catch { foundSeparador = normalizedText.Contains(sep); }
                    }
                    else
                    {
                        string[] variants = {
                            sep,
                            sep.Replace("DE ", ""),
                            sep.Replace("SEPARADOR ", ""),
                            "SEPARADOR",
                            "DOCUMENTOS SEPARADOR",
                            "HOJA SEPARADORA"
                        };
                        foreach (var v in variants)
                        {
                            if (normalizedText.Contains(v))
                            {
                                foundSeparador = true;
                                separadorVariants.Add(v);
                                break;
                            }
                        }
                    }
                }

                if (!string.IsNullOrEmpty(cod))
                {
                    if (UseRegex)
                    {
                        try
                        {
                            foundCodigo = Regex.IsMatch(normalizedText, cod, CaseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase);
                        }
                        catch { foundCodigo = normalizedText.Contains(cod); }
                    }
                    else
                    {
                        string[] patterns = { @"DOC-\d+", @"DOC\d+", @"DOC\s+\d+", @"DOC\s*-\s*\d+" };
                        foreach (string pat in patterns)
                        {
                            var matches = Regex.Matches(pageText, pat, RegexOptions.IgnoreCase);
                            if (matches.Count > 0)
                            {
                                foundCodigo = true;
                                codigoMatches.AddRange(matches.Select(m => m.Value));
                                break;
                            }
                        }
                        if (!foundCodigo && normalizedText.Contains(cod))
                        {
                            foundCodigo = true;
                            codigoMatches.Add(cod);
                        }
                    }
                }

                bool shouldRemove = foundSeparador || foundCodigo;
                List<string> diag = new List<string>();
                if (foundSeparador) diag.Add($"'{separadorVariants.FirstOrDefault() ?? sep}' encontrado");
                if (foundCodigo)
                {
                    string codes = string.Join(", ", codigoMatches.Take(3));
                    if (codigoMatches.Count > 3) codes += $" (+{codigoMatches.Count - 3} más)";
                    diag.Add($"Códigos: {codes}");
                }
                if (!diag.Any()) diag.Add("No se encontraron criterios");

                string preview = pageText.Length > 200 ? pageText[..200].Replace('\n', ' ') + "..." : pageText.Replace('\n', ' ');
                string normPreview = normalizedText.Length > 200 ? normalizedText[..200] + "..." : normalizedText;
                return (shouldRemove, $"{string.Join("; ", diag)} | Normalizado: {normPreview}");
            }
            catch (Exception ex)
            {
                return (false, $"Error en análisis: {ex.Message}");
            }
        }

        private async void TestAnalysis(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "PDF files|*.pdf|All files|*.*", Title = "Seleccionar PDF para análisis" };
            if (ofd.ShowDialog() != DialogResult.OK) return;

            byte[] pdfBytes = await File.ReadAllBytesAsync(ofd.FileName);
            var (shouldRemove, diagnosis) = AnalyzeFirstPageContent(pdfBytes);
            string result = $"📄 PDF: {Path.GetFileName(ofd.FileName)}\n" +
                            $"📊 Resultado: {(shouldRemove ? "✅ REMOVER primera página" : "❌ CONSERVAR primera página")}\n" +
                            $"🔍 Detalles: {diagnosis}";
            lblTestResult.Text = result;
            Log($"🧪 Prueba de análisis: {diagnosis}", shouldRemove ? "warning" : "info");
            MessageBox.Show(result, "Resultado de Análisis");
        }

        private void ShowNormalizationExample(object sender, EventArgs e)
        {
            string example = @"
        EJEMPLOS DE NORMALIZACIÓN DE TEXTO:

        TEXTO ORIGINAL:
        ""Sépárádör de Dócüméntös
        Código: DOC-001
        Fecha: 15/01/2024""

        TEXTO NORMALIZADO:
        ""SEPARADOR DE DOCUMENTOS CODIGO DOC 001 FECHA 15 01 2024""

        ---

        TEXTO ORIGINAL:
        ""HOJA   SEPARADORA  
        de  documentos varios
        Ref: DOC - 123""

        TEXTO NORMALIZADO:
        ""HOJA SEPARADORA DE DOCUMENTOS VARIOS REF DOC 123""

        ---

        PATRONES QUE SE DETECTARÁN:

        1. ""SEPARADOR DE DOCUMENTOS"" detectará:
           • ""Sépárádör de Dócüméntös""
           • ""SEPARADOR DOCUMENTOS""  
           • ""separador de documentos""
           • ""Separador De Documentos""
           • ""Hoja separadora""

        2. ""DOC-"" detectará:
           • ""DOC-001""
           • ""DOC001""
           • ""DOC 001""
           • ""DOC - 001""
           • ""doc-123""
           • ""Documento-456""
        ";
            MessageBox.Show(example, "Ejemplo de Normalización");
        }

        // ---------- PDF Manipulation (PDFsharp) ----------
        private byte[] RemovePdfFirstPage(byte[] pdfBytes)
        {
            try
            {
                using var inputStream = new MemoryStream(pdfBytes);
                using var document = PdfReader.Open(inputStream, PdfDocumentOpenMode.Modify);

                if (document.Pages.Count <= 1)
                    return pdfBytes;

                document.Pages.RemoveAt(0);

                using var outputStream = new MemoryStream();
                document.Save(outputStream, false);
                return outputStream.ToArray();
            }
            catch (Exception ex)
            {
                Log($"  ⚠️ Error procesando PDF con PDFsharp: {ex.Message}", "warning");
                return pdfBytes;
            }
        }

        // ---------- API Update ----------
        private async Task<bool> UpdateDocumentApi(JObject documentData, List<(byte[] bytes, string mimeType)> processedBinaries,
                                                   string documentId, string documentVersionId)
        {
            try
            {
                string url = $"{BaseUrl}app/documents";

                using var client = new HttpClient();
                client.DefaultRequestHeaders.Add("x-api-key", ApiKey);
                client.DefaultRequestHeaders.Add("x-api-secret", ApiSecret);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BearerToken);
                client.DefaultRequestHeaders.Add("Cookie", Cookie);

                if (processedBinaries.Count == 0) return false;
                var binary = processedBinaries[0];
                string base64 = Convert.ToBase64String(binary.bytes);
                string dataUrl = $"data:{binary.mimeType};base64,{base64}";

                var docInfo = documentData["document"] as JObject ?? documentData;
                string docTypeId = docInfo["documentTypeId"]?.ToString() ?? "68ed1507639d4d046c985249";

                var payload = new JObject
                {
                    ["documentTypeId"] = docTypeId,
                    ["contentType"] = "application/pdf",
                    ["data"] = docInfo["data"] ?? new JObject(),
                    ["documents"] = new JArray(dataUrl),
                    ["mustUpdateBinaries"] = true,
                    ["parentDocumentVersionId"] = documentVersionId,
                    ["filesName"] = docInfo["filesName"] ?? new JArray(),
                    ["originMethod"] = docInfo["originMethod"]?.ToString() ?? "imported"
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                Log($"  📤 Enviando a {url}", "info");
                var response = await client.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    Log($"  ✅ Éxito - Código: {response.StatusCode}", "success");
                    return true;
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    Log($"  ❌ Error - Código: {response.StatusCode}", "error");
                    Log($"  📋 Respuesta: {error[..Math.Min(200, error.Length)]}", "error");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log($"  ❌ Excepción: {ex.Message}", "error");
                return false;
            }
        }

        // ---------- Processing ----------
        private async void ProcessSelectedOnly(object sender, EventArgs e)
        {
            if (processing) return;

            var selectedDocs = FoundDocuments
                .Where(doc =>
                {
                    string id = doc["documentId"]?.ToString() ?? doc["id"]?.ToString() ?? "";
                    return SelectedDocuments.ContainsKey(id) && SelectedDocuments[id];
                })
                .ToList();

            if (!selectedDocs.Any())
            {
                MessageBox.Show("No hay documentos seleccionados. Selecciona al menos uno.", "Sin selección");
                return;
            }

            processing = true;
            btnProcess.Enabled = false;
            btnStop.Enabled = true;
            cancellationTokenSource = new CancellationTokenSource();

            UpdateStatus(0, 0, 0, 0, 0, 0, "Iniciando...");
            Log($"🚀 Iniciando procesamiento de {selectedDocs.Count} documentos seleccionados...", "system");

            await Task.Run(() => ProcessDocuments(selectedDocs, cancellationTokenSource.Token));

            processing = false;
            btnProcess.Enabled = true;
            btnStop.Enabled = false;
        }

        private void StopProcessing(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
            Log("⏹️ Proceso detenido por el usuario", "warning");

            int processed = int.TryParse(lblProcessed.Text, out int p) ? p : 0;
            int updated = int.TryParse(lblUpdated.Text, out int u) ? u : 0;
            int errors = int.TryParse(lblErrors.Text, out int er) ? er : 0;
            int removed = int.TryParse(lblRemoved.Text, out int rm) ? rm : 0;
            int skipped = int.TryParse(lblSkipped.Text, out int sk) ? sk : 0;
            double progress = progressBar.Value;

            UpdateStatus(processed, updated, errors, removed, skipped, progress, "Proceso detenido");
        }

        private async Task ProcessDocuments(List<JObject> docs, CancellationToken ct)
        {
            string outputDir = "documentos_procesados";
            Directory.CreateDirectory(outputDir);

            int total = docs.Count;
            int processed = 0, updated = 0, errors = 0, pagesRemoved = 0, skipped = 0;

            var headers = new Dictionary<string, string>
            {
                ["x-api-key"] = ApiKey,
                ["x-api-secret"] = ApiSecret,
                ["Authorization"] = $"Bearer {BearerToken}",
                ["Cookie"] = Cookie,
                ["Accept"] = "application/json"
            };

            for (int i = 0; i < docs.Count; i++)
            {
                if (ct.IsCancellationRequested) break;
                var doc = docs[i];
                string documentId = doc["documentId"]?.ToString() ?? doc["id"]?.ToString() ?? $"Documento_{i + 1}";
                string documentName = doc["name"]?.ToString() ?? $"Documento_{i + 1}";
                string documentTypeId = doc["documentTypeId"]?.ToString() ?? doc["typeId"]?.ToString() ?? "Desconocido";
                string documentVersionId = doc["lastDocumentVersionId"]?.ToString() ?? doc["documentVersionId"]?.ToString() ?? "";

                if (string.IsNullOrEmpty(documentVersionId) && DocumentVersionIds.ContainsKey(documentId))
                    documentVersionId = DocumentVersionIds[documentId];

                Log($"\n📂 Procesando documento {i + 1}/{total}: {documentName}", "system");
                Log($"  📋 Document ID: {documentId}", "info");
                Log($"  📋 Version ID: {documentVersionId ?? "(no disponible)"}", "info");
                Log($"  📋 Type ID: {documentTypeId}", "info");

                UpdateStatus(processed, updated, errors, pagesRemoved, skipped, (double)i / total * 100, $"Procesando {i + 1}/{total}: {documentId[..Math.Min(8, documentId.Length)]}...");

                try
                {
                    // If no version, try to fetch versions
                    if (string.IsNullOrEmpty(documentVersionId))
                    {
                        Log($"  ℹ️ No se encontró documentVersionId. Intentando obtener versiones...", "warning");
                        string versionsUrl = $"{BaseUrl}documents/{documentId}/versions";
                        using var client = new HttpClient();
                        foreach (var h in headers) client.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);
                        var resp = await client.GetAsync(versionsUrl);
                        if (resp.IsSuccessStatusCode)
                        {
                            string json = await resp.Content.ReadAsStringAsync();
                            var versionsData = JToken.Parse(json);
                            if (versionsData is JArray arr && arr.Count > 0)
                                documentVersionId = arr.Last["documentVersionId"]?.ToString() ?? arr.Last["id"]?.ToString() ?? "";
                            else if (versionsData is JObject obj)
                            {
                                foreach (var key in new[] { "versions", "data", "results" })
                                {
                                    if (obj[key] is JArray arr2 && arr2.Count > 0)
                                    {
                                        documentVersionId = arr2.Last["documentVersionId"]?.ToString() ?? arr2.Last["id"]?.ToString() ?? "";
                                        if (!string.IsNullOrEmpty(documentVersionId)) break;
                                    }
                                }
                            }
                            if (!string.IsNullOrEmpty(documentVersionId))
                                Log($"  ✅ Se obtuvo documentVersionId: {documentVersionId}", "success");
                        }
                        if (string.IsNullOrEmpty(documentVersionId))
                        {
                            Log($"  ❌ No se pudo obtener documentVersionId de las versiones", "error");
                            errors++; continue;
                        }
                    }

                    // Get document details
                    string detailUrl = $"{BaseUrl}documents/{documentId}/versions/{documentVersionId}";
                    using var clientDetail = new HttpClient();
                    foreach (var h in headers) clientDetail.DefaultRequestHeaders.TryAddWithoutValidation(h.Key, h.Value);
                    var detailResp = await clientDetail.GetAsync(detailUrl);
                    if (!detailResp.IsSuccessStatusCode)
                    {
                        Log($"  ❌ Error al obtener detalles: {detailResp.StatusCode}", "error");
                        errors++; continue;
                    }

                    string detailJson = await detailResp.Content.ReadAsStringAsync();
                    var documentData = JObject.Parse(detailJson);

                    string metadataFile = Path.Combine(outputDir, $"{documentId}_metadata.json");
                    await File.WriteAllTextAsync(metadataFile, detailJson, Encoding.UTF8);
                    Log($"  💾 Metadatos guardados en: {metadataFile}", "info");

                    var binaries = new List<JToken>();
                    if (documentData["document"] is JObject docInfo)
                    {
                        if (docInfo["binaries"] is JArray arr) binaries = arr.ToList();
                    }
                    else if (documentData["binaries"] is JArray arr) binaries = arr.ToList();

                    if (!binaries.Any())
                    {
                        Log($"  ℹ️ Sin archivos adjuntos encontrados en este documento", "warning");
                        skipped++; processed++; continue;
                    }

                    var processedBinaries = new List<(byte[] bytes, string mimeType)>();
                    bool anyPageRemoved = false;

                    for (int j = 0; j < binaries.Count; j++)
                    {
                        var binaryToken = binaries[j];
                        Log($"  📎 Procesando binario {j + 1}/{binaries.Count}...", "info");

                        string binaryStr = binaryToken.ToString();
                        if (binaryStr.StartsWith("data:"))
                        {
                            string[] parts = binaryStr.Split(new[] { ',' }, 2);
                            string header = parts[0];
                            string encoded = parts.Length > 1 ? parts[1] : "";
                            string mime = header.Split(':')[1].Split(';')[0];

                            if (mime.ToLower().Contains("pdf"))
                            {
                                byte[] fileData = Convert.FromBase64String(encoded);
                                if (SaveOriginals)
                                {
                                    string origPath = Path.Combine(outputDir, $"{documentId}_original_{j + 1}.pdf");
                                    await File.WriteAllBytesAsync(origPath, fileData);
                                    Log($"  💾 Original guardado: {origPath}", "info");
                                }

                                bool shouldRemove = false;
                                string analysis = "Sin análisis";

                                if (RemoveFirstPage)
                                {
                                    if (AnalyzeFirstPage)
                                    {
                                        (shouldRemove, analysis) = AnalyzeFirstPageContent(fileData);
                                        Log($"  🔍 Análisis de primera página: {analysis}", "analysis");
                                    }
                                    else
                                    {
                                        shouldRemove = true;
                                        analysis = "Remoción automática (sin análisis)";
                                        Log($"  🔍 Se removerá la primera página sin análisis", "info");
                                    }
                                }

                                byte[] processedData = fileData;
                                if (shouldRemove)
                                {
                                    processedData = RemovePdfFirstPage(fileData);
                                    if (processedData != fileData)
                                    {
                                        pagesRemoved++;
                                        anyPageRemoved = true;
                                        Log($"  ✅ Primera página removida (PDFsharp)", "success");
                                    }
                                }

                                processedBinaries.Add((processedData, mime));

                                string processedPath = Path.Combine(outputDir, $"{documentId}_procesado_{j + 1}.pdf");
                                await File.WriteAllBytesAsync(processedPath, processedData);
                                Log($"  💾 Procesado guardado: {processedPath}", "info");

                                // Update page count in grid (PdfPig)
                                try
                                {
                                    using var ms = new MemoryStream(processedData);
                                    using var pdf = PdfPigDoc.Open(ms);
                                    int pages = pdf.NumberOfPages;
                                    Invoke(new Action(() =>
                                    {
                                        foreach (DataGridViewRow row in dgvResults.Rows)
                                        {
                                            if (row.Tag?.ToString() == documentId)
                                            {
                                                row.Cells["pages"].Value = pages;
                                                row.Cells["analysis"].Value = shouldRemove ? "✅ Removida" : "❌ Conservada";
                                                break;
                                            }
                                        }
                                    }));
                                }
                                catch { }
                            }
                            else
                            {
                                Log($"  ⚠️ Binario {j + 1} no es PDF ({mime}), se conservará sin cambios", "warning");
                                byte[] fileData = Convert.FromBase64String(encoded);
                                processedBinaries.Add((fileData, mime));
                            }
                        }
                        else
                        {
                            Log($"  ⚠️ Binario {j + 1} no es un data URL válido", "warning");
                        }
                    }

                    if (UpdateInApi && anyPageRemoved && processedBinaries.Any())
                    {
                        Log($"  📡 Actualizando documento en la API...", "system");
                        bool success = await UpdateDocumentApi(documentData, processedBinaries, documentId, documentVersionId);
                        if (success)
                        {
                            updated++;
                            Log($"  ✅ Documento actualizado en la API", "success");
                        }
                        else
                        {
                            errors++;
                            Log($"  ❌ Error al actualizar en la API", "error");
                        }
                    }
                    else if (!anyPageRemoved)
                    {
                        Log($"  ℹ️ No se requirió actualización (sin páginas removidas)", "info");
                        skipped++;
                    }

                    processed++;
                    Log($"  ✅ Documento procesado exitosamente", "success");
                }
                catch (Exception ex)
                {
                    Log($"  ❌ Error procesando documento: {ex.Message}", "error");
                    errors++;
                }

                UpdateStatus(processed, updated, errors, pagesRemoved, skipped, (double)(i + 1) / total * 100);
                await Task.Delay(500);
            }

            Log($"\n===========================================================================================================", "system");
            Log($"✅ Proceso completado", "success");
            Log($"📊 Resumen:", "system");
            Log($"  • Documentos procesados: {processed}", "info");
            Log($"  • Documentos actualizados: {updated}", "info");
            Log($"  • Páginas removidas: {pagesRemoved}", "info");
            Log($"  • Documentos saltados: {skipped}", "skipped");
            Log($"  • Errores: {errors}", errors > 0 ? "error" : "info");
            Log($"📁 Carpeta: {outputDir}", "system");

            UpdateStatus(processed, updated, errors, pagesRemoved, skipped, 100, "Proceso completado");
        }

        private void UpdateStatus(int processed, int updated, int errors, int removed, int skipped, double progress, string status = "")
        {
            Invoke(new Action(() =>
            {
                lblProcessed.Text = processed.ToString();
                lblUpdated.Text = updated.ToString();
                lblErrors.Text = errors.ToString();
                lblRemoved.Text = removed.ToString();
                lblSkipped.Text = skipped.ToString();
                progressBar.Value = (int)progress;
                if (!string.IsNullOrEmpty(status))
                    lblStatus.Text = status;
            }));
        }

        // ---------- Utilities ----------
        private void TestConnection(object sender, EventArgs e)
        {
            MessageBox.Show("Prueba de conexión no implementada.", "Información");
        }

        private void ShowAbout(object sender, EventArgs e)
        {
            string about = @"
        Procesador Inteligente de Documentos PDF - Prodoctivity

        Versión: 3.1 (C# con PDFsharp + PdfPig)
        Descripción: Herramienta para buscar, analizar, seleccionar y procesar documentos PDF

        Características:
        • Búsqueda automática al iniciar (muestra TODOS los documentos)
        • Análisis inteligente de primera página con normalización de texto (PdfPig)
        • Remoción selectiva basada en contenido (PDFsharp)
        • Copiar Type IDs de documentos seleccionados
        • Selección individual o masiva de documentos
        • Actualizar documentos en la API
        • Conservar toda la información original

        Desarrollado para automatización de procesos documentales.";
            MessageBox.Show(about, "Acerca de");
        }

        private void SaveConfig(object sender, EventArgs e)
        {
            var config = new JObject
            {
                ["baseurl"] = BaseUrl,
                ["apiKey"] = ApiKey,
                ["apiSecret"] = ApiSecret,
                ["bearer_token"] = BearerToken,
                ["cookie"] = Cookie,
                ["search_document_type_ids"] = SearchDocumentTypeIds,
                ["analysis_keyword_separador"] = KeywordSeparador,
                ["analysis_keyword_codigo"] = KeywordCodigo,
                ["analysis_normalize_text"] = NormalizeText,
                ["analysis_remove_accents"] = RemoveAccents,
                ["analysis_remove_punctuation"] = RemovePunctuation,
                ["analysis_remove_extra_spaces"] = RemoveExtraSpaces,
                ["analysis_ignore_line_breaks"] = IgnoreLineBreaks,
                ["analysis_case_sensitive"] = CaseSensitive,
                ["analysis_use_regex"] = UseRegex
            };

            SaveFileDialog sfd = new SaveFileDialog { Filter = "JSON files|*.json", DefaultExt = "json" };
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(sfd.FileName, config.ToString(Formatting.Indented));
                Log($"✅ Configuración guardada en: {sfd.FileName}", "success");
            }
        }

        private void LoadConfig(object sender, EventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog { Filter = "JSON files|*.json" };
            if (ofd.ShowDialog() == DialogResult.OK)
            {
                string json = File.ReadAllText(ofd.FileName);
                var config = JObject.Parse(json);

                BaseUrl = config["baseurl"]?.ToString() ?? BaseUrl;
                ApiKey = config["apiKey"]?.ToString() ?? ApiKey;
                ApiSecret = config["apiSecret"]?.ToString() ?? ApiSecret;
                BearerToken = config["bearer_token"]?.ToString() ?? BearerToken;
                Cookie = config["cookie"]?.ToString() ?? Cookie;
                SearchDocumentTypeIds = config["search_document_type_ids"]?.ToString() ?? SearchDocumentTypeIds;
                KeywordSeparador = config["analysis_keyword_separador"]?.ToString() ?? KeywordSeparador;
                KeywordCodigo = config["analysis_keyword_codigo"]?.ToString() ?? KeywordCodigo;
                NormalizeText = config["analysis_normalize_text"]?.Value<bool>() ?? NormalizeText;
                RemoveAccents = config["analysis_remove_accents"]?.Value<bool>() ?? RemoveAccents;
                RemovePunctuation = config["analysis_remove_punctuation"]?.Value<bool>() ?? RemovePunctuation;
                RemoveExtraSpaces = config["analysis_remove_extra_spaces"]?.Value<bool>() ?? RemoveExtraSpaces;
                IgnoreLineBreaks = config["analysis_ignore_line_breaks"]?.Value<bool>() ?? IgnoreLineBreaks;
                CaseSensitive = config["analysis_case_sensitive"]?.Value<bool>() ?? CaseSensitive;
                UseRegex = config["analysis_use_regex"]?.Value<bool>() ?? UseRegex;

                // Update UI
                txtBaseUrl.Text = BaseUrl;
                txtApiKey.Text = ApiKey;
                txtApiSecret.Text = ApiSecret;
                txtBearerToken.Text = BearerToken;
                txtCookie.Text = Cookie;
                txtSearchTypeIds.Text = SearchDocumentTypeIds;
                txtKeywordSeparador.Text = KeywordSeparador;
                txtKeywordCodigo.Text = KeywordCodigo;
                chkNormalizeText.Checked = NormalizeText;
                chkRemoveAccents.Checked = RemoveAccents;
                chkRemovePunctuation.Checked = RemovePunctuation;
                chkRemoveExtraSpaces.Checked = RemoveExtraSpaces;
                chkIgnoreLineBreaks.Checked = IgnoreLineBreaks;
                chkCaseSensitive.Checked = CaseSensitive;
                chkUseRegex.Checked = UseRegex;

                Log($"✅ Configuración cargada desde: {ofd.FileName}", "success");
            }
        }

        private void OpenOutputFolder(object sender, EventArgs e)
        {
            string dir = "documentos_procesados";
            if (Directory.Exists(dir))
                System.Diagnostics.Process.Start("explorer.exe", dir);
            else
                MessageBox.Show("La carpeta de documentos no existe aún.", "Información");
        }
    }
}