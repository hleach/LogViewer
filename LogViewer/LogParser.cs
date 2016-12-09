using System;
using System.Data;
using System.IO;
using System.Text;
using MSUtil;

namespace RPowerLogViewer
{
    class LogParser
    {

        public static DataTable runQuery(string q, string context, Func<int, bool> updateCallback = null)
        {
            Object o = null;

            switch (context.ToLower())
            {
                case "active directory":
                    o = new COMADSInputContextClassClass();
                    break;
                case "iis binary":
                    o = new COMIISBINInputContextClassClass();
                    break;
                case "csv file":
                    o = new COMCSVInputContextClassClass();
                    break;
                case "windows trace":
                    o = new COMETWInputContextClassClass();
                    break;
                case "windows events":
                    o = new COMEventLogInputContextClassClass();
                    break;
                case "file system":
                    o = new COMFileSystemInputContextClassClass();
                    break;
                case "http error":
                    o = new COMHttpErrorInputContextClassClass();
                    break;
                case "iis":
                    o = new COMIISIISInputContextClassClass();
                    break;
                case "iis odbc":
                    o = new COMIISODBCInputContextClassClass();
                    break;
                case "iis w3c":
                    o = new COMIISW3CInputContextClassClass();
                    break;
                case "iis ncsa":
                    o = new COMIISNCSAInputContextClassClass();
                    break;
                case "netmon":
                    o = new COMNetMonInputContextClassClass();
                    break;
                case "registry":
                    o = new COMRegistryInputContextClassClass();
                    break;
                case "textline":
                    o = new COMTextLineInputContextClassClass();
                    break;
                case "textword":
                    o = new COMTextWordInputContextClassClass();
                    break;
                case "tsv file":
                    o = new COMTSVInputContextClassClass();
                    break;
                case "urlscan":
                    o = new COMURLScanLogInputContextClassClass();
                    break;
                case "w3c":
                    o = new COMW3CInputContextClassClass();
                    break;
                case "xml file":
                    o = new COMXMLInputContextClassClass();
                    break;
                case "rpower logs":
                    o = Activator.CreateInstance(Type.GetTypeFromProgID("MSUtil.LogQuery.RPower.RPowerLogs"));
                    break;
                case "rpower keys":
                    o = Activator.CreateInstance(Type.GetTypeFromProgID("MSUtil.LogQuery.RPower.RPowerKeys"));
                    break;
                case "rpower cc logs":
                    o = Activator.CreateInstance(Type.GetTypeFromProgID("MSUtil.LogQuery.RPower.RPowerCC"));
                    break;
                case "rpower dbf":
                    o = Activator.CreateInstance(Type.GetTypeFromProgID("MSUtil.LogQuery.RPower.RPowerDB"));
                    break;
                default:
                    o = Activator.CreateInstance(Type.GetTypeFromProgID(context));
                    break;
            }

            if (o == null)
                return null;
            else return runQuery(q, o, updateCallback);
        }

        // Returns a populated DataTable or null if query throws an exception.
        // Can throw COMException on SQL syntax error
        public static DataTable runQuery(string q, object context, Func<int, bool> updateCallback = null)
        {
            var dt = new DataTable();
            var lp = new LogQueryClassClass();
            int count = 0;
            var run = true;

            try
            {
                var ilr = lp.Execute(q, context);

                for (int i = 0; i < ilr.getColumnCount(); i++)
                    dt.Columns.Add(new DataColumn(ilr.getColumnName(i)));

                while (!ilr.atEnd() && run)
                {
                    var rec = ilr.getRecord();
                    string[] row = new string[ilr.getColumnCount()];

                    for (int i = 0; i < ilr.getColumnCount(); i++)
                        row[i] = rec.getValue(i).ToString();

                    dt.Rows.Add(row);

                    // Only update every ~50 times to keep from lagging up the process too much.
                    if (updateCallback != null && (count++ % 53 == 0))
                        run = updateCallback(count);

                    ilr.moveNext();
                }

                if (updateCallback != null)
                    updateCallback(count);

                ilr.close();
            }
            catch
            {
                // LogParser doesn't call CloseInput on some sql syntax exceptions.
                // This can cause files to remain locked. So, we cleanup and rethrow.
                dynamic obj = (dynamic)context;

                if (obj.GetType().GetMethod("CloseInput") != null)
                    obj.CloseInput(true);

                throw;
            }

            return dt;
        }

        private static string CSVQuote(string s)
        {
            if (s.IndexOf(',') < 0 && s.IndexOf('"') < 0)
                return s;

            var sb = new StringBuilder();

            sb.Append('"');

            foreach (char c in s)
                if (c == '"')
                    sb.Append("\"\"");
                else sb.Append(c);

            sb.Append('"');

            return sb.ToString();
        }

        internal static void SaveQueryCSV(DataTable dt, string fileName)
        {
            if (dt.Columns.Count < 1)
                return;

            var file = new StreamWriter(fileName);

            file.Write(CSVQuote(dt.Columns[0].ColumnName));
            for (int i = 1; i < dt.Columns.Count; i++)
                file.Write(",{0}", CSVQuote(dt.Columns[i].ColumnName));
            file.WriteLine();

            foreach (DataRow row in dt.Rows)
            {
                file.Write(CSVQuote(row[0].ToString()));
                for (int i = 1; i < dt.Columns.Count; i++)
                    file.Write(",{0}", CSVQuote(row[i].ToString()));
                file.WriteLine();
            }

            file.Close();
        }

    }
}
