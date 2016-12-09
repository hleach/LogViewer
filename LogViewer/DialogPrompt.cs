using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPowerLogViewer
{
    public partial class DialogPrompt : Form
    {
        Label textLabel;
        TextBox textBox;
        Button okButton;

        public DialogPrompt(string caption = "Prompt", string text = "OK?")
        {
            InitializeComponent();

            Width = 350;
            Height = 145;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            Text = caption;

            textLabel = new Label() { Left = 20, Top = 20, Width = 300, Text = text };
            textBox = new TextBox() { Left = 20, Top = 45, Width = 300 };
            okButton = new Button() { Text = "OK", Left = 240, Width = 80, Top = 75, DialogResult = DialogResult.OK };
            okButton.Click += (s, e) => { this.Close(); };

            Controls.Add(textLabel);
            Controls.Add(textBox);
            Controls.Add(okButton);
        }

        public string get()
        {
            return textBox.Text;
        }

    }
}
