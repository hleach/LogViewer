using System;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Reflection;
using System.Windows.Forms;

namespace RPowerLogViewer
{
    public struct QueryStruct
    {
        public string name;
        public string context;
        public string query;
    };

    public partial class QueryControl : UserControl
    {
        private string queryContext = "";
        private string queryName = "";
        private DataTable dataTable = null;
        private DataTable messageTable = null;
        private BackgroundWorker bgwTableBuilder = null;
        private bool queryRunning = false;

        internal QueryControl()
        {
            InitializeComponent();
            queryDataGridView.CellFormatting += new DataGridViewCellFormattingEventHandler(FormatDataGridViewCell);
            queryDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            queryDataGridView.EnableHeadersVisualStyles = false;

            // Enable double buffering on queryDataGridView
            typeof(DataGridView).InvokeMember("DoubleBuffered",
                BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty,
                null, queryDataGridView, new object[] { true });
        }

        internal QueryControl(string query, string context, string name)
        {
            InitializeComponent();
            queryContext = context;
            queryTextBox.Text = query;
            queryName = name;
            queryDataGridView.CellFormatting += new DataGridViewCellFormattingEventHandler(FormatDataGridViewCell);
            queryDataGridView.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            queryDataGridView.EnableHeadersVisualStyles = false;

            // Enable double buffering on queryDataGridView
            typeof(DataGridView).InvokeMember("DoubleBuffered", 
            BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.SetProperty, 
                null, queryDataGridView, new object[] { true });
        }

        private void FormatDataGridViewCell(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if ((e.RowIndex & 1) == 0)
                e.CellStyle.BackColor = Color.FromArgb(255, 245, 203);
            else e.CellStyle.BackColor = Color.FromArgb(255, 255, 234);
        }

        private void ResizeDataGridView()
        {
            // Silliness required to fit fields to the data and still allow modification of field widths
            // We have to run later in gui thread and then from there make another call to run later
            // and undo what we did.
            if (queryDataGridView.Columns.Count > 0)
            {
                BeginInvoke((Action)(() =>
                {
                    queryDataGridView.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;

                    BeginInvoke((Action)(() =>
                    {
                        foreach (DataGridViewColumn col in queryDataGridView.Columns)
                        {
                            var width = col.Width;
                            col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                            col.Width = width;
                        }
                    }));
                }));
            }
        }

        internal void RunQuery()
        {
            queryRunning = true;
            bgwTableBuilder = new BackgroundWorker();
            bgwTableBuilder.DoWork +=
                new DoWorkEventHandler(bgwTableBuilder_DoWork);
            bgwTableBuilder.RunWorkerCompleted +=
                new RunWorkerCompletedEventHandler(bgwTableBuilder_Complete);
            bgwTableBuilder.RunWorkerAsync();
        }

        private void bgwTableBuilder_DoWork(object sender, DoWorkEventArgs e)
        {
            dataTable = null;
            
            PostMessage("Records", "0");

            try
            {
                dataTable = LogParser.runQuery(queryTextBox.Text, queryContext, WorkerUpdate);
            }
            catch (Exception ex)
            {
                PostMessage(" ", ex.Message);
            }
        }

        private void bgwTableBuilder_Complete(object sender, RunWorkerCompletedEventArgs e)
        {
            if (dataTable != null)
            {
                BeginInvoke((Action)(() =>
                {
                    queryDataGridView.DataSource = dataTable;
                    ResizeDataGridView();
                }));
            }

            queryRunning = false;
        }

        private void PostMessage(string header, string message)
        {
            messageTable = new DataTable();
            messageTable.Columns.Add(new DataColumn(header));
            messageTable.Rows.Add(new string[] { message });

            BeginInvoke((Action)(() =>
            {
                queryDataGridView.DataSource = messageTable;
                ResizeDataGridView();
            }));
        }

        private void UpdateMessage(string message)
        {
            if (messageTable != null)
                if (messageTable.Rows.Count > 0)
                    messageTable.Rows[0].SetField(0, message);
        }
        
        private bool WorkerUpdate(int count)
        {
            messageTable.Rows[0].SetField(0, count.ToString());

            return queryRunning;
        }

        public DataTable FlipDataTable(DataTable dt)
        {
            DataTable newTable = new DataTable();

            DataColumn firstColumn = new DataColumn(dt.Columns[0].ColumnName);
            newTable.Columns.Add(firstColumn);

            //Add a column for each row in first data table
            for (int i = 0; i < dt.Rows.Count; i++)
            {
                DataColumn dc = new DataColumn(dt.Rows[i][0].ToString());
                newTable.Columns.Add(dc);
            }

            for (int j = 1; j < dt.Columns.Count; j++)
            {
                DataRow dr = newTable.NewRow();
                dr[0] = dt.Columns[j].ColumnName;

                for (int k = 0; k < dt.Rows.Count; k++)
                {
                    dr[k + 1] = dt.Rows[k][j];
                }

                newTable.Rows.Add(dr);
            }

            return newTable;
        }

        internal QueryStruct GetQuery()
        {
            QueryStruct q;

            q.name = queryName;
            q.context = queryContext;
            q.query = queryTextBox.Text; 
            return q;
        }

        private void queryTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && e.Control)
            {
                e.SuppressKeyPress = true;
                this.RunQuery();
            }
        }

        internal void SetName(string name)
        {
            queryName = name;
        }

        internal void StopQuery()
        {
            queryRunning = false;
        }

        internal DataTable GetDataTable()
        {
            if (queryDataGridView.DataSource is DataTable)
                return (DataTable)queryDataGridView.DataSource;
            else return null;
        }
    }
}
