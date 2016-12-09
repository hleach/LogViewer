using System;
using System.Text;
using System.Windows.Forms;
using System.IO;

namespace RPowerLogViewer
{
    public partial class MainView : Form
    {
        private static readonly string INI_NEWLINE = "\x1e";

        string[] QUERY_TYPES = {
            "RPower Logs", "RPower CC Logs", "RPower Keys", "RPower DBF",
            "CSV File", "TSV File", "XML File", "File System", "Windows Events",
            "Registry", "Textline", "Textword", "Netmon", "Windows Trace",
            "Active Directory", "IIS", "IIS Binary", "IIS ODBC", "IIS W3C",
            "IIS NCSA", "HTTP Error", "Urlscan", "W3C"
        };

        public void addQueryTab(string queryType, string q, string tabname = "")
        {
            var tp = new TabPage();
            var qc = new QueryControl(q, queryType, tabname);
            qc.Dock = DockStyle.Fill;
            qc.Name = "Query";
            if (tabname == "")
                tp.Text = queryType;
            else tp.Text = tabname;
            tp.Controls.Add(qc);
            queryTabControl.TabPages.Add(tp);
            queryTabControl.SelectedTab = tp;
        }

        public bool updateQueryTree(QueryStruct q, bool saveNew = false)
        {
            TreeNode contextNode = null;

            foreach (TreeNode n in contextTreeView.Nodes)
            {
                if (n.Text == q.context)
                {
                    contextNode = n;
                    foreach (QueryTreeNode qn in n.Nodes)
                    {
                        if (qn.Text == q.name)
                        {
                            qn.query = q.query;
                            return true;
                        }

                    }
                }
            }

            if (saveNew && contextNode != null)
            {
                var queryNode = new QueryTreeNode();
                queryNode.Text = q.name;
                queryNode.query = q.query;
                contextNode.Nodes.Add(queryNode);
            }

            return false;
        }

        public MainView()
        {
            InitializeComponent();
            loadQueryTree();
        }

        private void saveQueryTree(string name = "LogViewer.ini")
        {
            var sb = new StringBuilder();

            foreach(TreeNode n in contextTreeView.Nodes)
            {
                sb.Append("[").Append(n.Text).Append("]").Append(Environment.NewLine);
                foreach (QueryTreeNode qn in n.Nodes)
                    sb.Append("   ")
                        .Append(qn.Text)
                        .Append("=")
                        .Append(qn.query.Replace(Environment.NewLine, INI_NEWLINE))
                        .Append(Environment.NewLine);
                sb.Append(Environment.NewLine);
            }

            File.WriteAllText(name, sb.ToString());
        }

        private void loadQueryTree()
        {
            TreeNode currentQueryType = null;

            StreamReader ini = null;

            try
            {
                ini = new StreamReader("LogViewer.ini");

                while (!ini.EndOfStream)
                {
                    var s = ini.ReadLine().Trim();

                    if (s.Length >= 2)
                    {
                        if (s[0] == '[' && s[s.Length - 1] == ']')
                        {
                            // New Query Type
                            currentQueryType = new TreeNode(s.Substring(1, s.Length - 2));
                            contextTreeView.Nodes.Add(currentQueryType);
                        }
                        else if (s.IndexOf('=') >= 0)
                        {
                            // New query
                            if (currentQueryType == null)
                                Console.Error.WriteLine("MainView(), currentQueryType == null: {0}", s);
                            else
                            {
                                string[] split = { "", "" };

                                split[0] = s.Substring(0, s.IndexOf("="));
                                split[1] = s.Substring(s.IndexOf("=") + 1).Replace(INI_NEWLINE, Environment.NewLine);

                                var queryNode = new QueryTreeNode();
                                queryNode.Text = split[0];
                                queryNode.query = split[1];
                                currentQueryType.Nodes.Add(queryNode);
                            }
                        }
                        else
                        {
                            // Junk
                            Console.Error.WriteLine("MainView(), bad line: {0}", s);
                        }
                    }
                    else
                    {
                        if (s.Length > 0)
                            Console.Error.WriteLine("MainView(), short line: {0}", s);
                    }
                }

            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                // Anything goes wrong, we load up default query types.
                foreach (var s in QUERY_TYPES)
                    contextTreeView.Nodes.Add(s);
            }
            finally
            {
                if (ini != null)
                    ini.Close();
            }
        }

        // Opens a new query on doubleclicking tree node.
        private void contextTreeView_DoubleClick(object sender, EventArgs e)
        {
            if (contextTreeView.SelectedNode is QueryTreeNode)
            {
                QueryTreeNode qtn = (QueryTreeNode)contextTreeView.SelectedNode;
                addQueryTab(qtn.Parent.Text, qtn.query, qtn.Text);
            }
            else addQueryTab(contextTreeView.SelectedNode.Text, "");
        }

        // Update query tree and save to default ini file.
        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (queryTabControl.SelectedTab != null)
            {
                var query = ((QueryControl)queryTabControl.SelectedTab.Controls["Query"]).GetQuery();

                updateQueryTree(query);
                saveQueryTree();
            }
        }

        // Closes a QueryControl. 
        private void closeTabToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (queryTabControl.SelectedTab != null)
            {
                ((QueryControl)queryTabControl.SelectedTab.Controls["Query"]).StopQuery();
                queryTabControl.TabPages.Remove(queryTabControl.SelectedTab);
            }
        }

        // Run the currently selected query tab.
        private void runToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (queryTabControl.SelectedTab != null)
                ((QueryControl)queryTabControl.SelectedTab.Controls["Query"]).RunQuery();
        }

        // Save the currently selected tab as a new saved query.
        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (queryTabControl.SelectedTab != null)
            {
                var query = ((QueryControl)queryTabControl.SelectedTab.Controls["Query"]).GetQuery();
                var prompt = new DialogPrompt("Save As?", "Please name this query:");

                if (prompt.ShowDialog() == DialogResult.OK)
                {
                    query.name = prompt.get();
                    updateQueryTree(query, saveNew: true);
                    saveQueryTree();
                    ((QueryControl)queryTabControl.SelectedTab.Controls["Query"]).SetName(query.name);
                    queryTabControl.SelectedTab.Text = query.name;
                }
            }
        }

        // Stops processing records on the currently selected query.
        private void stopQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (queryTabControl.SelectedTab != null)
                ((QueryControl)queryTabControl.SelectedTab.Controls["Query"]).StopQuery();
        }

        // Exits the program
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (queryTabControl.SelectedTab != null)
            {
                var queryControl = (QueryControl)queryTabControl.SelectedTab.Controls["Query"];

                if (queryControl.GetDataTable() != null)
                {
                    var sfd = new SaveFileDialog();

                    sfd.FileName = queryControl.GetQuery().name + "-" + DateTime.Now.ToString("yyyyMMddHHmm") + ".csv";
                    sfd.Filter = "CSV (*.csv)|*.*";
                    sfd.ShowDialog();
                    Console.WriteLine(sfd.FileName);

                    LogParser.SaveQueryCSV(queryControl.GetDataTable(), sfd.FileName);
                }
            }

        }
    }
}
