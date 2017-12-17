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
        private byte[] Data;
        private volatile int Pos;
        private volatile int Summ;
        private volatile int Count;
        private Thread backProgress;

        async private Task MakeAsyncRequest(string url, Label label)
        {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = WebRequestMethods.Http.Get;
            request.Timeout = 20000;
            request.Proxy = null;
            Task<WebResponse> task = Task.Factory.FromAsync(request.BeginGetResponse, asyncResult => request.EndGetResponse(asyncResult), (object)null);
            label.Text = "Идет процесс скачивания. Подождите!";
            await task.ContinueWith(t => {
                Stream stream = t.Result.GetResponseStream();
                Data = new byte[t.Result.ContentLength];
                backProgress = new Thread(ProcessData);
                backProgress.Start();
                while (Count < t.Result.ContentLength)
                {
                    int read = stream.Read(Data, Count, (int)t.Result.ContentLength - Count);   // возвращает скорлько байт было прочитано
                    lock (Data)
                        Count += read;
                }
            });
            label.Text = "Процесс завершен";
        }
        #region Механизм блокировки
        bool ProgressFinish()
        {
            lock (Data)
            {
                return Pos >= Data.Length;
            }
        }

        bool HasMoreData(out int DataSize)
        {
            lock (Data)
            {
                if (Count < Data.Length)
                    DataSize = (Count - Pos) & -4; // побитово сравнивает с -4(1...100)доп.код | если сравнивать 4(0100) с 7(0111) будет 4(0100), если 7(0111) с 8(1000) будет 0(0000)
                else                               // почему именно с -4 непонятно
                    DataSize = Count - Pos;
                return DataSize > 0;
            }
        }
        #endregion
        delegate void UpdateProgress();




        private void ProcessData()
        {
            int data_to_process;
            while (!ProgressFinish())
            {
                if (HasMoreData(out data_to_process))
                {
                    for (int i = Pos; i < Pos + (data_to_process & -4); i += 4)
                        Summ += Data[i] + (Data[i + 1] << 8) + (Data[i + 2] << 16) + (Data[i + 3] << 24);
                    switch (data_to_process & 3)
                    {
                        case 1:
                            Summ += Data[Pos + (data_to_process & -4)];
                            break;
                        case 2:
                            Summ += Data[Pos + (data_to_process & -4)] + (Data[Pos + (data_to_process & -4) + 1] << 8);
                            break;
                        case 3:
                            Summ += Data[Pos + (data_to_process & -4)] + (Data[Pos + (data_to_process & -4) + 1] << 8) + (Data[Pos + (data_to_process & -4) + 2] << 16);
                            break;
                    }
                    Pos += data_to_process;
                    progressBar.BeginInvoke(new UpdateProgress(() => progressBar.Value = (int)Math.Round(100.0 * Pos / (int)Data.Length)));
                }
                else
                    Thread.Sleep(50);
            }
        



      /*  private void ProcessData()          // здесь и не понятно, откуда берутся цифры, откуда берется отрицательное число
        {
            int data_to_process;
            while (!ProgressFinished())  // пока !(когда data свободна от другого потока, если pos >= длине data) | если pos меньше data.Lenght то идем в цикл
            {
                if (HasMoreData(out data_to_process))   
                {
                    for (int i = pos; i < pos + (data_to_process & -4); i += 4) // делит по байтам, остаток просчитывается ниже где switch
                        summ += data[i] + (data[i + 1] << 8) + (data[i + 2] << 16) + (data[i + 3] << 24); // << оператор сдвига сдвигает на N битов влево. Каждое слагаемое это байт
                    switch (data_to_process & 3)    // смотрит какой остаток
                    {
                        case 1:
                            summ += data[pos + (data_to_process & -4)];
                            break;
                        case 2:
                            summ +=(byte)( data[pos + (data_to_process & -4)] + (data[pos + (data_to_process & -4) + 1] << 8));
                            break;
                        case 3:
                            summ += (byte)(data[pos + (data_to_process & -4)] + (data[pos + (data_to_process & -4) + 1] << 8) + (data[pos + (data_to_process & -4) + 2] << 16));
                            break;
                    }
                    pos += data_to_process;
                    progressBar.BeginInvoke(new UpdateProgress(() => progressBar.Value = (int)Math.Round(100.0 * pos / (int)data.Length)));
                }
                else
                    Thread.Sleep(50);
            }*/
            

        }

        private async void button1_Click(object sender, EventArgs e)
        {
            label2.Text = "Загрузка";
            textBox2.Clear();
            textBox3.Clear();
            progressBar.Value = 0;
            Pos = 0;
            Data = new byte[0];
            Summ = 0;
            Count = 0;
            try
            {
                await MakeAsyncRequest(textBox1.Text, label2);
                textBox3.Text = String.Format("0x{0:X}", Summ);
                textBox2.Text = Summ.ToString();

            }
            catch
            {
                MessageBox.Show("Введите верный URL");

            }
            textBox1.Clear();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Dispose();
            Application.Exit();
        }
    }
    }
