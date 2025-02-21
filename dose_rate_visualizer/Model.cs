using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VMS.TPS.Common.Model.API;
using VMS.TPS.Common.Model.Types;

namespace dose_rate_visualizer.Models
{

    class DoseRateStat
    {
        public DoseRateStat(List<double> doserates) {
            double sum = 0.0;
            double sum_of_squares = 0.0;

            (sum, sum_of_squares) = doserates.Aggregate((sum, sum_of_squares), (acc, x) => (acc.sum += x, acc.sum_of_squares +=  Math.Pow(x, 2)));

            double mean = sum / doserates.Count;
            double sd = Math.Sqrt(sum_of_squares / doserates.Count - Math.Pow(mean, 2));
            double mode = doserates.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key;

            Mean = mean;
            Sd = sd;
            Mode = mode;
        }

        public String Print()
        {
            //return String.Format("\t平均: {0:F2}\n\t標準偏差: {1:F2}\n\t最頻値: {2:F2}", Mean, Sd, Mode);
            return String.Format("    平均: {0:F2}\n    標準偏差: {1:F2}\n    最頻値: {2:F2}", Mean, Sd, Mode);

        }

        private double Mean = 0.0;
        private double Sd = 0.0;
        private double Mode = 0.0;
    }

    internal class Model
    {
        private ScriptContext Context { get; set; }

        /* dose rate related properties */
        public List<List<double>> DoseRates { get; } = new List<List<double>>();
        public List<List<double>> GantryAngles { get; } = new List<List<double>>();
        public List<List<double>> MetersetWeights { get; } = new List<List<double>>();
        public List<Beam> TreatmentFields { get; } = new List<Beam>();


        public List<DoseRateStat> DoseRateStats { get; } = new List<DoseRateStat>();


        public void SetScriptContext(ScriptContext _context)
        {
            Context = _context;

            /* get control point related values */
            var plan = Context.ExternalPlanSetup;
            var fields = plan.Beams;

            foreach (var field in fields)
            {
                if (field.IsSetupField == false)
                {
                    TreatmentFields.Add(field);

                    var metersets = new List<double>();
                    var angles = new List<double>();

                    var control_points = field.ControlPoints;
                    foreach (var cp in control_points)
                    {
                        metersets.Add(cp.MetersetWeight);
                        angles.Add(cp.GantryAngle);
                    }

                    MetersetWeights.Add(metersets);
                    GantryAngles.Add(angles);

                    //var buf = String.Format("{0}, metersets.Count: {1}, angles.Count: {2}", field.Id, metersets.Count, angles.Count);
                    //MessageBox.Show(buf);
                }
                else { }

            }

            /* Calculate dose rate and its statistics */
            var total_dose_rates = new List<double>();
            for (int i = 0; i < TreatmentFields.Count; ++i)
            {
                var field = TreatmentFields.ElementAt(i);
                var beam_dose_rate = field.DoseRate;
                var beam_mu = field.Meterset.Value;
                var field_dose_rates = calc_dose_rates(MetersetWeights[i], GantryAngles[i], beam_dose_rate, beam_mu, field.GantryDirection);
                DoseRates.Add(field_dose_rates);
                total_dose_rates.AddRange(field_dose_rates);

                //MessageBox.Show(String.Format("F{0}, beam_dose_rate: {1}, beam_mu: {2}, field_dose_rate.Count: {3}, total_dose_rate.Count: {4}"
                    //, i + 1, beam_dose_rate, beam_mu, field_dose_rates.Count, total_dose_rates.Count));

            }
            DoseRates.Add(total_dose_rates);
            //MessageBox.Show(String.Format("DoseRates.Count: {0}", DoseRates.Count));


            foreach (var dr in DoseRates)
            {
                DoseRateStats.Add(new DoseRateStat(dr));
            }

        }


        public List<double> calc_dose_rates(List<double> metersets, List<double> gantry_angles, double beam_doserate, double beam_mu, GantryDirection direction)
        {
            const double MAX_SPEED_DEG_PER_S = 6.0;
            const double MAX_SPEED_DEG_PER_MIN = MAX_SPEED_DEG_PER_S * 60.0;
            var doserates = new List<double> ();
            List<double> meterset_diff = new List<double> ();
            List<double> angle_diff = new List<double>();

            if (metersets.Count != gantry_angles.Count)
            {
                var buf = String.Format("count is not match: meterset.Count = {0}, gantry_angles.Count = {1}", metersets.Count, gantry_angles.Count);
                MessageBox.Show(buf);
            }

            for (int i = 1; i < metersets.Count; ++i)
            {
                meterset_diff.Add(metersets[i] - metersets[i - 1]);

                var prev_angle = gantry_angles[i - 1];
                var curr_angle = gantry_angles[i];
                var diff = 0.0;
                if (direction == GantryDirection.Clockwise)
                {
                    if (curr_angle - prev_angle < 0)    // around 0 deg 
                    {
                        diff = (curr_angle + 360) - prev_angle;
                    }
                    else
                    {
                        diff = curr_angle - prev_angle;
                    }
                }
                else
                {
                    if (prev_angle - curr_angle < 0)    // around 0 deg 
                    {
                        diff = (prev_angle + 360) - curr_angle;
                    }
                    else
                    {
                        diff = prev_angle - curr_angle;
                    }
                }
                angle_diff.Add(diff);
            }

            var cp_mu = meterset_diff.Select(x => x * beam_mu).ToList();
            var mu_per_deg = cp_mu.Zip(angle_diff, (mu, angle) => mu / angle).ToList();
            doserates = mu_per_deg.Select(x => (x * MAX_SPEED_DEG_PER_MIN > beam_doserate) ? beam_doserate : x * MAX_SPEED_DEG_PER_MIN).ToList();

            return doserates;

        }

    }
}
