using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Threading;
using System.Threading.Tasks;

using log4net;

using cat.View;
using cat.DB;
using cat.Model;

namespace cat {
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window {
        /// <summary>
        /// View Model
        /// </summary>
        private CatsViewModel vm = new CatsViewModel();

        /// <summary>
        /// DB Context
        /// </summary>
        private CatObservationContext CatCntext = new CatObservationContext();

        /// <summary>
        /// Loaded Cats Data
        /// </summary>
        List<Cat> CatMaster;


        /// <summary>
        /// Get Log4j Logger
        /// Initialize from Self Class Info
        /// </summary>
        private ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindow() {
            InitializeComponent();
            this.DataContext = vm;
            this.GetCatMasters();
            btnErase_Click(btnErase, new RoutedEventArgs());
        }

        /// <summary>
        /// Show Cat Observation Data to Editing 
        /// </summary>
        /// <param name="sender">Cause Object</param>
        /// <param name="e">Event Object</param>
        private void dgCatObservation_CurrentCellChanged(object sender, EventArgs e) {
            int RowCnt = dgCatObservation.Items.IndexOf(dgCatObservation.CurrentItem); 
            if (RowCnt >= 0) {
                vm.CatEdit = vm.CatObservations[RowCnt];
            }
        }

        /// <summary>
        /// Clear Entry
        /// </summary>
        /// <param name="sender">Cause Object</param>
        /// <param name="e">Event Object</param>
        private void btnErase_Click(object sender, RoutedEventArgs e) {
            vm.ClearEdit();
        }

        /// <summary>
        /// Select Filtered Cat Observation Data
        /// </summary>
        /// <param name="sender">Cause Object</param>
        /// <param name="e">Event Object</param>
        /// <remarks> Get Filter Args from Input Area </remarks>
        private async void btnSearch_Click(object sender, RoutedEventArgs e) {
            logger.Info("btnSearch_Click - start");
            btnSearch.IsEnabled = false;
            try {
                // Create Select Query
                CatObservationSearch filter = vm.GetEntry();
                var query = CreateCatObservationsQuery(filter);
                try {
                    // Create Cancel Source
                    var tokenSource = new CancellationTokenSource();
                    var cancelToken = tokenSource.Token;
                    // Execute Fetch
                    var task = GetCatObservationsAsyncTask(query, cancelToken);
                    if (await Task.WhenAny(task, Task.Delay(250)) != task) { // when over 250msec 
                        // Show splash window
                        var window = new SplashWindow(task, tokenSource);
                        window.Owner = this;
                        window.ShowDialog();
                    }
                } catch (Exception ex) {
                    logger.Error("SearchExecuteException:" + ex.Message);
                }
            } finally {
                btnSearch.IsEnabled = true;
                logger.Info("btnSearch_Click - end");
            }
        }

        /// <summary>
        /// Get Cat Master List
        /// </summary>
        private void GetCatMasters() {
            CatMaster = CatCntext.Cats.Where(x => x.LostFlag == null).OrderBy(x => x.CatId).ToList();
            vm.SetCatMasters(CatMaster);
        }

        /// <summary>
        /// Create Cat Observateion select query
        /// </summary>
        /// <param name="filter"></param>
        /// <returns></returns>
        private dynamic CreateCatObservationsQuery(CatObservationSearch filter) {
            // TODO: JOIN CatObservations And Cats
            var query = from co in CatCntext.CatObservations
                        join cm in CatCntext.Cats on co.CatId equals cm.CatId
                        where cm.LostFlag == null
                        select new {
                            ObservateDate = co.ObservateDate,
                            ObservateTime = co.ObservateTime,
                            CatId = cm.CatId,
                            CatName = co.CatName,
                            HairPattern = co.HairPattern,
                            Gender = co.Gender,
                            BodyType = co.BodyType,
                            FaceType = co.FaceType,
                            Age = co.Age,
                            Personality = co.Personality,
                            Country = co.Country,
                            Area = co.Area
                        };
            switch (filter.ObservateDateFilter) {
                case ObservateDateFilterType.Up:
                    if (filter.ObservateDate != null)
                        query = query.Where(x => x.ObservateDate >= filter.ObservateDate);
                    if (filter.ObservateTime != null)
                        query = query.Where(x => x.ObservateTime >= filter.ObservateTime);
                    break;
                case ObservateDateFilterType.Down:
                    if (filter.ObservateDate != null)
                        query = query.Where(x => x.ObservateDate <= filter.ObservateDate);
                    if (filter.ObservateTime != null)
                        query = query.Where(x => x.ObservateTime <= filter.ObservateTime);
                    break;
                default:
                    if (filter.ObservateDate != null)
                        query = query.Where(x => x.ObservateDate == filter.ObservateDate);
                    if (filter.ObservateTime != null)
                        query = query.Where(x => x.ObservateTime == filter.ObservateTime);
                    break;
            }
            // TODO: Create Filter
            if (filter.CatId > 0) {
                query = query.Where(x => x.CatId == filter.CatId);
            }
            if (!string.IsNullOrEmpty(filter.CatName?.Trim())) {
                query = query.Where(x => x.CatName.IndexOf(filter.CatName) >= 0);
            }
            if (!string.IsNullOrEmpty(filter.HairPattern?.Trim())) {
                query = query.Where(x => x.HairPattern.IndexOf(filter.HairPattern) >= 0);
            }
            if (!string.IsNullOrEmpty(filter.FaceType?.Trim())) {
                query = query.Where(x => x.FaceType.IndexOf(filter.FaceType) >= 0);
            }
            if (!string.IsNullOrEmpty(filter.Personality?.Trim())) {
                query = query.Where(x => x.Personality.IndexOf(filter.Personality) >= 0);
            }
            if (!string.IsNullOrEmpty(filter.Country?.Trim())) {
                query = query.Where(x => x.Country.IndexOf(filter.Country) >= 0);
            }
            if (!string.IsNullOrEmpty(filter.Area?.Trim())) {
                query = query.Where(x => x.Area.IndexOf(filter.Area) >= 0);
            }
            if (filter.UseGender) {
                query = query.Where(x => x.Gender == filter.Gender);
            }
            if (filter.UseBodyType) {
                query = query.Where(x => x.BodyType == filter.BodyType);
            }
            if (filter.UseAge) {
                query = query.Where(x => x.Age == filter.Age);
            }
            query = query.OrderBy(x => x.ObservateDate).ThenBy(x => x.ObservateTime).ThenBy(x => x.CatId);
            return query;

        }
        /// <summary>
        /// Set Cat Observation List Task
        /// </summary>
        /// <param name="query"> select query </param>
        private async Task<bool> GetCatObservationsAsyncTask(dynamic query, CancellationToken cancelToken) {
            var context = SynchronizationContext.Current;
            return await Task.Run(() => {
                logger.Info("GetCatObservationsAsyncTask - start");
                List<CatObservationDisplay> dspList = new List<CatObservationDisplay>();
                // Is Canceled ? -> Except
                cancelToken.ThrowIfCancellationRequested();
                try {
                    foreach (var cat in query) {
                        if (cancelToken.IsCancellationRequested) {
                            return false;
                        }
#if DEBUG
                        Thread.Sleep(1000);
#endif
                        CatObservationDisplay dsp = new CatObservationDisplay();
                        dsp.ObservateDate = cat.ObservateDate;
                        dsp.ObservateTime = cat.ObservateTime;
                        dsp.CatId = cat.CatId;
                        dsp.CatName = cat.CatName;
                        dsp.HairPattern = cat.HairPattern;
                        dsp.Gender = cat.Gender;
                        dsp.BodyType = cat.BodyType;
                        dsp.FaceType = cat.FaceType;
                        dsp.Age = cat.Age;
                        dsp.Personality = cat.Personality;
                        dsp.Country = cat.Country;
                        dsp.Area = cat.Area;
                        dspList.Add(dsp);
                    }
                    vm.CatObservations = dspList;
                    return true;
                } finally {
                    logger.Info("GetCatObservationsAsyncTask - end");
                    // TODO: Touch UI Thread
                    context.Post(__ => {
                        btnSearch.IsEnabled = true;
                    }, null);
                }
            }, cancelToken);
        }

        /// <summary>
        /// Save Cat Observation List File
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnFileSave_Click(object sender, RoutedEventArgs e) {
            // 1.Save File Name collect
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            // initialize Directory (MyDoc)
            dlg.InitialDirectory = System.Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            dlg.Title = "Select Save File";
            dlg.Filter = "CSV(*.csv)|*.csv";

            if (dlg.ShowDialog() == true) {
                // Call SaveFileDialog
                try {
                    // 2. File Save
                    // 2.1 Create StreamWriter
                    using (var sw = new System.IO.StreamWriter(dlg.FileName, false, System.Text.Encoding.GetEncoding("Shift_JIS"))) {
                        // 2.2 Each Grid Datas Write
                        Func<string, string> dqot = (str) => { return "\"" + str.Replace("\"", "\"\"") + "\""; };
                        foreach (var d in vm.CatObservations)
                            sw.WriteLine(dqot(d.ObservateDate?.ToShortDateString()) + "," + dqot(d.ObservateTime?.ToShortTimeString()) + "," + dqot(d.CatName) + "," + dqot(d.HairPattern) + "," + dqot(d.GenderText) + "," + dqot(d.BodyTypeText) + "," + dqot(d.FaceType) + "," + dqot(d.AgeText) + "," + dqot(d.Country) + "," + dqot(d.Area));
                    }
                    MessageBox.Show("Output Complete", "Cat Observation", MessageBoxButton.OK, MessageBoxImage.Information);
                } catch (SystemException ex) {
                    // -> Save Error log
                    WriteEventLog("FileOutputError", true);
                    logger.Error("File OutputError:" + ex.Message);
                    // -> Error Message Show
                    MessageBox.Show(ex.Message, "Cat Observation", MessageBoxButton.OK, MessageBoxImage.Error);

                }
            }
        }

        /// <summary>
        /// Write Windows EventLog Message
        /// </summary>
        /// <param name="logString"> Log Text </param>
        /// <param name="ErrorOrInfo"> </param>
        private void WriteEventLog(string logString, bool ErrorOrInfo) {
            // Write Entry
            EventLog.WriteEntry(
                "Cat", "CatObservation:" + logString,
                ErrorOrInfo ? EventLogEntryType.Error : EventLogEntryType.Information);
        }

    }
}
