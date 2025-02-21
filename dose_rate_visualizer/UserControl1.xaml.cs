using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

using dose_rate_visualizer.ViewModels;
using ScottPlot;

namespace VMS.TPS
{
    /// <summary>
    /// UserControl1.xaml の相互作用ロジック
    /// </summary>
    public partial class Script : UserControl
    {
        private const Int32 HIST_BIN_WIDTH = 50;
        public Script()
        {
            InitializeComponent();
        }

        public void Execute(ScriptContext context, System.Windows.Window window)
        {

            window.Height = 500;
            window.Width = 800;
            window.Content = this;
            window.Background = Brushes.WhiteSmoke;
            window.SizeChanged += (sender, args) =>
            {
                this.Height = window.ActualHeight * 0.90;
                this.Width = window.ActualWidth * 0.98;
            };


            var view_model = this.DataContext as ViewModel;
            view_model.SetScriptContextToModel(context);

            Loaded += (s, e) =>
            {
                /* scatter plot of DoseRate */
                var dose_rates = view_model.InstModel.DoseRates;
                var gantry_angles = view_model.InstModel.GantryAngles;
                var treatment_fields = view_model.InstModel.TreatmentFields;

                for (int i = 0; i < gantry_angles.Count; ++i)
                {
                    var rate = dose_rates[i].ToArray();
                    var angle = gantry_angles[i].Skip(1/*to match array length with doserate*/).ToArray();

                    /* for ScottPlot v4 */
                    var scatter = scatter_plot.Plot.AddScatter(angle, rate, label: treatment_fields.ElementAt(i).Id);

                }

                /* for ScottPlot v4 */
                scatter_plot.Plot.XAxis.Label("Gantry angle [deg]");
                scatter_plot.Plot.YAxis.Label("Dose rate [MU/min]");
                scatter_plot.Plot.AxisAuto();
                scatter_plot.Plot.Legend();
                scatter_plot.Refresh();

                var hist = ScottPlot.Statistics.Histogram.WithFixedBinSize(min: 0, max: treatment_fields.First().DoseRate, binSize: HIST_BIN_WIDTH);
                hist.AddRange(dose_rates.Last());
                // Display the histogram as a bar plot
                var bar_plot = hist_plot.Plot.AddBar(values: hist.Counts, positions: hist.Bins);
                bar_plot.BarWidth = HIST_BIN_WIDTH;
                bar_plot.Label = "Total";

                // Customize the style of each bar
                bar_plot.BorderLineWidth = 0;
                bar_plot.FillColor = System.Drawing.Color.ForestGreen;

                // Customize plot style
                hist_plot.Plot.YLabel("Counts");
                hist_plot.Plot.XLabel("Dose rate [MU/min]");

                hist_plot.Plot.AxisAuto();
                hist_plot.Plot.Legend();
                hist_plot.Refresh();
            };

            /* Print Statistics */
            var stat = view_model.InstModel.DoseRateStats;
            string buf = "線量率 [MU/min]\n\n";
            for (int i = 0; i < stat.Count - 1; ++i)
            {
                buf += String.Format("F{0}\n{1}\n", i + 1, stat[i].Print());
            }
            buf += String.Format("Total\n{0}\n", stat.Last().Print());

            stat_text.Text = buf;

            //var plt = wpfplot_amp.Plot;
            //plt.Style(figureBackground: System.Drawing.Color.WhiteSmoke,
            //    dataBackground: System.Drawing.Color.WhiteSmoke);
        }
    }
}
