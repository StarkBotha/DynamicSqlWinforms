using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace dynaforms1
{
    public partial class frmMain : Form
    {
        string scriptpath = string.Empty;

        string varStart = "<variables>";
        string varEnd = "</variables>";
        string nameStart = "<name>";
        string nameEnd = "</name>";
        string connStringStart = "<connstring>";
        string connStringEnd = "</connstring>";

        List<ListedScript> scripts = new List<ListedScript>();
        List<FieldItem> fieldItems = new List<FieldItem>();

        public frmMain()
        {
            InitializeComponent();
        }

        private void btnAddLabel_Click(object sender, EventArgs e)
        {
            var label1 = new Label();
            label1.Text = "I am a label";
            panel1.Controls.Add(label1);
        }

        private void btnAddPicker_Click(object sender, EventArgs e)
        {
            var dtp = new DateTimePicker();
            dtp.Format = DateTimePickerFormat.Custom;
            dtp.CustomFormat = "yyyy/MM/dd HH:mm:ss";
            panel1.Controls.Add(dtp);
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            scriptpath = ConfigurationManager.AppSettings["path"];

            GetScriptList();
        }

        private void GetScriptList()
        {
            var scriptDir = new DirectoryInfo(scriptpath);
            var files = scriptDir.GetFiles();



            foreach (var file in files)
            {
                var scriptFile = new ListedScript();
                var text = File.ReadAllText(file.FullName);
                var nameLocation = text.IndexOf(nameStart);
                var nameEndLocation = text.IndexOf(nameEnd);

                if (nameLocation > -1)
                {
                    var nameLength = text.IndexOf(nameEnd) - (nameEnd.Length + nameLocation);
                    var name = text.Substring(nameLocation + nameStart.Length, nameEndLocation - (nameLocation + nameStart.Length));


                    var connstringlocation = text.IndexOf(connStringStart);
                    var connstringendlocation = text.IndexOf(connStringEnd);

                    if (connstringlocation > -1)
                    {
                        var connstring = text.Substring(connstringlocation + connStringStart.Length, connstringendlocation - (connstringlocation + connStringStart.Length));



                        cmbScripts.Items.Add(new ListedScript()
                        {
                            Name = name,
                            Path = file.FullName,
                            ConnString = connstring
                        });
                    }
                    else
                    {
                        rtbOutput.we($"No connection string defined for file: {file.FullName}");
                    }
                }
                else
                {
                    rtbOutput.we($"No name defined for file: {file.FullName}");
                    continue;
                }
            }
        }

        private void btnExecute_Click(object sender, EventArgs e)
        {
            var selectedScript = (ListedScript)cmbScripts.SelectedItem;

            var text = File.ReadAllText(selectedScript.Path);

            var connstring = ConfigurationManager.ConnectionStrings[selectedScript.ConnString].ConnectionString;

            using (var connection = new SqlConnection(connstring))
            {
                bool usereader = true;

                if (text.ToUpper().Contains("UPDATE ") || text.ToUpper().Contains("UPDATE "))
                {
                    usereader = false;
                    if (!text.ToUpper().Contains("WHERE"))
                    {
                        rtbOutput.we("UPDATE QUERY CONTAINS NO WHERE CLAUSE");
                        return;
                    }
                }
                else if (text.ToUpper().Contains("INSERT "))
                {
                    usereader = false;
                }


                if (usereader)
                {
                    using (var adapter = new SqlDataAdapter(text, connection))
                    {
                        foreach (var fielditem in fieldItems)
                        {
                            adapter.SelectCommand.Parameters.Add($"@{fielditem.Name}", GetDbType(fielditem.Type)).Value = GetValue(fielditem);
                        }
                        connection.Open();

                        DataTable dt = new DataTable();
                        adapter.Fill(dt);
                        dgResults.DataSource = dt;
                        if (dt.Rows.Count == 0)
                        {
                            rtbOutput.we("No results.");
                            tabControl1.SelectedTab = tabControl1.TabPages["tabOutput"];
                        }
                        else
                        {
                            tabControl1.SelectedTab = tabControl1.TabPages["tabData"];
                        }
                    }
                }
                else
                {
                    using (var command = new SqlCommand(text, connection))
                    {
                        foreach (var fielditem in fieldItems)
                        {
                            command.Parameters.Add($"@{fielditem.Name}", GetDbType(fielditem.Type)).Value = GetValue(fielditem);
                        }

                        connection.Open();
                        rtbOutput.wl($"{command.ExecuteNonQuery()} rows affected.");
                    }
                }

                
            }
        }

        private SqlDbType GetDbType(string typestring)
        {
            if (typestring.StartsWith("VARCHAR"))
            {
                return SqlDbType.VarChar;
            }
            else if (typestring == "UNIQUEIDENTIFIER")
            {
                return SqlDbType.UniqueIdentifier;
            }
            else if (typestring == "BIT")
            {
                return SqlDbType.Bit;
            }
            else if (typestring == "DATETIME")
            {
                return SqlDbType.DateTime;
            }

            throw new Exception("Type not found");
        }

        private object GetValue(FieldItem fieldItem)
        {
            if (fieldItem.Type.StartsWith("VARCHAR"))
            {
                return ((TextBox)fieldItem.Control).Text;
            }
            else if (fieldItem.Type == "UNIQUEIDENTIFIER")
            {
                return Guid.Parse(((TextBox)fieldItem.Control).Text);
                //return ((TextBox)fieldItem.Control).Text;
            }
            else if (fieldItem.Type == "BIT")
            {
                return ((CheckBox)fieldItem.Control).Checked;
            }
            else if (fieldItem.Type == "DATETIME")
            {
                return ((DateTimePicker)fieldItem.Control).Value;
            }

            throw new Exception("Type not found");
        }

        private void btnLoad_Click(object sender, EventArgs e)
        {
            panel1.Controls.Clear();
            fieldItems.Clear();


            var selectedScript = (ListedScript)cmbScripts.SelectedItem;

            var text = File.ReadAllText(selectedScript.Path);

            rtbOutput.wl(text);

            var variablesLocation = text.IndexOf(varStart);
            var variableEndLocation = text.IndexOf(varEnd);
            if (variablesLocation > -1)
            {
                rtbOutput.wl("Found variables container");
                var variablesString = text.Substring(variablesLocation + varStart.Length, variableEndLocation - (variablesLocation + varStart.Length));
                rtbOutput.wl($"variable string: [{variablesString}]");

                var varlist = variablesString.Split('@');
                foreach (var variable in varlist)
                {
                    if (variable.Trim() == string.Empty) continue;
                    rtbOutput.wl($"variable definition: {variable}");
                    var varparams = variable.Split(':');
                    var fi = new FieldItem()
                    {
                        Name = varparams[0],
                        Type = varparams[1],
                        Label = varparams[2]
                    };

                    Label lbl = new Label();
                    lbl.Text = fi.Label;
                    lbl.AutoSize = true;
                    //lbl.Width = 150;
                    panel1.Controls.Add(lbl);

                    if (fi.Type.StartsWith("VARCHAR"))
                    {

                        var field = new TextBox();
                        field.Name = fi.Name;
                        field.Width = 250;
                        panel1.Controls.Add(field);
                        fi.Control = field;
                    }
                    else if (fi.Type == "UNIQUEIDENTIFIER")
                    {
                        var field = new TextBox();
                        field.Name = fi.Name;
                        field.Width = 250;
                        panel1.Controls.Add(field);
                        fi.Control = field;
                    }
                    else if (fi.Type == "BIT")
                    {
                        var field = new CheckBox();
                        field.Name = fi.Name;
                        field.Text = fi.Label;
                        field.Width = 250;
                        panel1.Controls.Add(field);
                        fi.Control = field;
                    }
                    else if (fi.Type == "DATETIME")
                    {
                        var field = new DateTimePicker();
                        field.Name = fi.Name;
                        field.Format = DateTimePickerFormat.Custom;
                        field.CustomFormat = "yyyy/MM/dd HH:mm:ss";
                        field.Width = 250;
                        panel1.Controls.Add(field);
                        fi.Control = field;
                    }

                    fieldItems.Add(fi);
                }
            }




        }
    }
}
