using System;
using System.Configuration;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.OneDrive.Sdk;
using Microsoft.OneDrive.Sdk.WindowsForms;
using System.IO;

namespace SaveFile
{
    public partial class Form1 : Form
    {
        private AccountSession accountSession = null;
        private AdalCredentialCache cache = null;
        private IOneDriveClient oneDriveClient = null;
        private string sourceFile = null;
        private bool authenticated = false;

        private async void Authenticate()
        {
            string clientId = ConfigurationManager.AppSettings["ida:ClientID"].ToString();
            string redirectUri = ConfigurationManager.AppSettings["ida:RedirectUri"].ToString();

            String credentials = Properties.Settings.Default.Credentials;
            if (credentials != null && credentials.Length > 0)
            {
                cache = new AdalCredentialCache(System.Convert.FromBase64String(credentials));
            }
            else
            {
                cache = new AdalCredentialCache();
            }

            BusinessAppConfig appConfig = new BusinessAppConfig();
            appConfig.ActiveDirectoryAppId = clientId;
            appConfig.ActiveDirectoryReturnUrl = redirectUri;
            oneDriveClient = BusinessClientExtensions.GetClient(appConfig, null, cache);

            AccountSession x = null;
            try {
                x = await oneDriveClient.AuthenticateAsync();
            }
            catch (OneDriveException ex)
            {
                if (ex.Error.Code.Equals("MyFilesCapabilityNotFound"))
                {
                    MessageBox.Show("Your account is not enabled for OneDrive.");
                } else
                {
                    MessageBox.Show(ex.Error.Message);
                }
                Close();
            }

            if (x != null)
            {
                accountSession = x;
                credentials = System.Convert.ToBase64String(cache.GetCacheBlob());
                Properties.Settings.Default.Credentials = credentials;
                Properties.Settings.Default.Save();
                authenticated = true;

                button1.Enabled = true;
                textFilename.Enabled = true;
                textFilename.Focus();
            }
        }

        public Form1()
        {
            InitializeComponent();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string[] args = Environment.GetCommandLineArgs();
            if (args.Length > 0)
                LoadFile(args[1]);
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            Visible = false;
            Authenticate();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            if (textFilename.Text.Length == 0)
                return;
            if (sourceFile == null || sourceFile.Length == 0)
                return;

            if (!oneDriveClient.IsAuthenticated)
                Authenticate();
            if (!oneDriveClient.IsAuthenticated)
            {
                Close();
                return;
            }

            Item rootItem = await oneDriveClient
                             .Drive
                             .Root
                             .Request()
                             .GetAsync();

            string uploadPath = "/drive/items/root:/" + Uri.EscapeUriString(textFilename.Text);
            FileStream stream = null;
            try {
                stream = new System.IO.FileStream(sourceFile, System.IO.FileMode.Open);
            }
            catch (FileNotFoundException ex)
            {
                MessageBox.Show("The source file does not exist.");
                return;
            }
            catch (UnauthorizedAccessException ex)
            {
                MessageBox.Show("You do not have permission to access the source file.");
                return;
            }
            try {
                var uploadedItem =
                                await
                                    this.oneDriveClient.ItemWithPath(uploadPath).Content.Request().PutAsync<Item>(stream);
                button1.Enabled = false;
                textFilename.Text = "";
                textFilename.Enabled = false;
                Visible = false;
            }
            catch (OneDriveException ex)
            {
                MessageBox.Show(ex.Error.Message);
            }

        }

        public void LoadFile(String filename)
        {
            sourceFile = filename;
            if (authenticated)
            {
                button1.Enabled = true;
                textFilename.Enabled = true;
                Visible = true;
            }
            textFilename.Text = Path.GetFileName(filename);
        }
    }
}
