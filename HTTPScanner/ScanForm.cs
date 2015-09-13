using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HTTPScanner
{
    public partial class ScanForm : Form
    {
        private bool scanning = false;
        private Scanner scanner;
        private int maxNumOfAsyncScanners = 200;
        private CancellationTokenSource cancellationTokenSource;
        private static int id = 0;
        private List<HttpStatusCode> allHttpStatuscodes;

        public ScanForm()
        {
            InitializeComponent();
            scanner = new Scanner();

            allHttpStatuscodes = new List<HttpStatusCode>();
            AddAllHttpStatuscodes();
        }

        private void AddAllHttpStatuscodes()
        {
            foreach (HttpStatusCode statusCode in Enum.GetValues(typeof(HttpStatusCode)))
                allHttpStatuscodes.Add(statusCode);
        }

        private async void startScanButton_Click(object sender, EventArgs e)
        {
            startScanButton.Enabled = false;
            scanning = true;
            while (scanning)
            {
                cancellationTokenSource = new CancellationTokenSource();
                var taskList = new List<Task<HttpResponseMessage>>();

                IEnumerable<Task<HttpResponseMessage>> enumerableTasks = from value in Enumerable.Range(0, maxNumOfAsyncScanners)
                                                                            select scanner.ScanIPAddressAsync(Scanner.GenerateIPAddress(), cancellationTokenSource.Token);
                Console.Write($"{id++}: Starting {maxNumOfAsyncScanners} Tasks:");
                var tasks = enumerableTasks.ToArray();
                var res = await Task.WhenAll(tasks);
                if (cancellationTokenSource.IsCancellationRequested)
                    Console.Write(" Tasks cancelled.");
                else
                    Console.Write(" Tasks completed.");
                Console.WriteLine();
                var httpResponseMessages = new List<HttpResponseMessage>();
                foreach (var v in tasks)
                {
                    if (v.Result == null)
                        continue;
                    httpResponseMessages.Add(v.Result);
                }
                FilterAndAddResponseMessages(httpResponseMessages);
                cancellationTokenSource = null;
            }
            startScanButton.Enabled = true;
        }

        private List<HttpStatusCode> BuildFilter()
        {
            var statusCodes = new List<HttpStatusCode>();

            if (anyHttpStatusCheckbox.Checked)
                return allHttpStatuscodes;

            if (okHttpStatusCheckbox.Checked)
                statusCodes.Add(HttpStatusCode.OK);
            if (badRequestHttpStatusCheckbox.Checked) 
                statusCodes.Add(HttpStatusCode.BadRequest);
            if (unauthorizedHttpStatusCheckbox.Checked)
                statusCodes.Add(HttpStatusCode.Unauthorized);

            return statusCodes;
        }

        private void FilterAndAddResponseMessages(List<HttpResponseMessage> msgs)
        {
            var acceptedCodes = BuildFilter();
            
            foreach (var msg in msgs)
            {
                if (acceptedCodes.Contains(msg.StatusCode))
                {
                    var address = msg.RequestMessage.RequestUri.Host.ToString();
                    var statuscode = msg.StatusCode.ToString();
                    var item = new ListViewItem(new string[] { address, statuscode });
                    resultList.Items.Add(item);
                    resultList.EnsureVisible(resultList.Items.Count - 1);
                }
            }
        }

        private void stopScanButton_Click(object sender, EventArgs e)
        {
            if (cancellationTokenSource != null)
            {
                scanning = false;
                cancellationTokenSource.Cancel();
            }
        }


        private void resultList_DoubleClick(object sender, EventArgs e)
        {
            var clickedItem = resultList.SelectedItems[0].SubItems[0].Text;
            System.Diagnostics.Process.Start("http://" + clickedItem);
        }
    }
}
