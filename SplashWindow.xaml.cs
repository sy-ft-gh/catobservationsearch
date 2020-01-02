using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace cat {

    /// <summary>
    /// SplashWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class SplashWindow : Window {
        private Task<bool> task;
        private CancellationTokenSource cnclSrc;
        public SplashWindow(Task<bool> t, CancellationTokenSource cancel) {
            InitializeComponent();

            this.task = t;
            this.cnclSrc = cancel;

            this.Worker = new BackgroundWorker();
            this.Worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
            this.Worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(Worker_RunWorkerCompleted);
            this.Worker.WorkerSupportsCancellation = true;
            this.Worker.RunWorkerAsync();
        }
        BackgroundWorker Worker;

        private void btnCancelImmadiately_Click(object sender, RoutedEventArgs e) {
            this.Worker.CancelAsync();
            if (!task.IsCompleted) {
                cnclSrc.Cancel();
            }
            this.Close();
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e) {
            do {
                Task.Delay(100);
            } while(!this.Worker.CancellationPending && !task.IsCompleted);
        }
        void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            btnCancelImmadiately_Click(this.Worker, new RoutedEventArgs());
        }

    }
}
