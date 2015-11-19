using System;
using System.ComponentModel;
using System.Drawing;
using System.Net;
using System.Net.Http;
using System.Windows.Forms;

namespace Chapter15
{
    [Description("Listing 15.01")]
    class AsyncForm : Form
	{
	    Label label;
	    Button button;
	
	    public AsyncForm()
	    {
	        label = new Label { Location = new Point(10, 20), Text = "Length" };
	        button = new Button { Location = new Point(10, 50), Text = "Click" };
	        button.Click += DisplayWebSiteLength;
	        AutoSize = true;
	        Controls.Add(label);
	        Controls.Add(button);
	    }
	
	    async void DisplayWebSiteLength(object sender, EventArgs e)
	    {
            label.Text = "Fetching...";
            using (HttpClient client = new HttpClient())
            {
                string text = await client.GetStringAsync(@"http://127.0.0.1:8080/FileFormatConverters.exe");
                label.Text = text.Length.ToString();
            }
            var x = 1;
	    }

        static void Main()
        {
            Application.Run(new AsyncForm());
        }
	}
}
