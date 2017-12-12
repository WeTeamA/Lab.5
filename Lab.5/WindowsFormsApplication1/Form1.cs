using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Net;
using System.Threading;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }
        private byte[] data;
        private volatile int pos;
        private volatile int summ;
        private volatile int count;
        private Thread backProgress;

        async private Task MakeAsyncRequest(string url, Label label)
        {
            label.Text = "Идет процесс скачивания. Подождите!";
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;
            Task<WebResponse> task = Task.Factory.FromAsync(request.BeginGetResponse, asyncResult => request.EndGetResponse(asyncResult), (object)null);

            await task.ContinueWith(t => {
                Stream rs = t.Result.GetResponseStream();
                data = new byte[t.Result.ContentLength];
                backProgress = new Thread(ProcessData);
                backProgress.Start();
                while (count < t.Result.ContentLength)
                {
                    int read = rs.Read(data, count, (int)t.Result.ContentLength - count);
                    lock (data)
                        count += read;
                }
            });
            label.Text = "Процесс завершен";
        }
        bool ProgressFinished()
        {
            lock (data)
            {
                return pos >= data.Length;
            }
        }

        bool HasMoreData(out int DataSize)
        {
            lock (data)
            {
                if (count < data.Length)
                    DataSize = (count - pos) & -4;
                else
                    DataSize = count - pos;
                return DataSize > 0;
            }
        }
        delegate void UpdateProgress();

        

        private void ProcessData()
        {
            int data_to_process;
            while (!ProgressFinished())
            {
                if (HasMoreData(out data_to_process))
                {
                    for (int i = pos; i < pos + (data_to_process & -4); i += 4)
                        summ += data[i] + (data[i + 1] << 8) + (data[i + 2] << 16) + (data[i + 3] << 24);
                    switch (data_to_process & 3)
                    {
                        case 1:
                            summ += data[pos + (data_to_process & -4)];
                            break;
                        case 2:
                            summ += data[pos + (data_to_process & -4)] + (data[pos + (data_to_process & -4) + 1] << 8);
                            break;
                        case 3:
                            summ += data[pos + (data_to_process & -4)] + (data[pos + (data_to_process & -4) + 1] << 8) + (data[pos + (data_to_process & -4) + 2] << 16);
                            break;
                    }
                    pos += data_to_process;
                    progressBar.BeginInvoke(new UpdateProgress(() => progressBar.Value = (int)Math.Round(100.0 * pos / (int)data.Length)));
                }
                else
                    Thread.Sleep(50);
            }


        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await MakeAsyncRequest(textBox1.Text, label2);
            textBox3.Text = String.Format("0x{0:X}", summ);
            textBox2.Text = summ.ToString();
        }
    }
    }
