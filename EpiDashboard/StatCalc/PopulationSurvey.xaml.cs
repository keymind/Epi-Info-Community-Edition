﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace EpiDashboard.StatCalc
{
    /// <summary>
    /// Interaction logic for PopulationSurvey.xaml
    /// </summary>
    public partial class PopulationSurvey : UserControl, IGadget, IStatCalcControl
    {
        public event GadgetRefreshedHandler GadgetRefreshed;
        public event GadgetClosingHandler GadgetClosing;
        public event GadgetProcessingFinishedHandler GadgetProcessingFinished;
        public event GadgetRepositionEventHandler GadgetReposition;
        public event GadgetCheckForCancellationHandler GadgetCheckForCancellation;
        public event GadgetEventHandler GadgetDragStart;
        public event GadgetEventHandler GadgetDragStop;
        public event GadgetEventHandler GadgetDrag;
        public event GadgetAnchorSetEventHandler GadgetAnchorSetFromXml;

        private bool isProcessing;

        public UserControl AnchorLeft { get; set; }
        public UserControl AnchorTop { get; set; }
        public UserControl AnchorBottom { get; set; }
        public UserControl AnchorRight { get; set; }
        public Guid UniqueIdentifier { get; private set; }

        public PopulationSurvey()
        {
            InitializeComponent();
            txtPopulationSize.TextChanged += new TextChangedEventHandler(txtInputs_TextChanged);
            txtExpectedFreq.TextChanged += new TextChangedEventHandler(txtInputs_TextChanged);
            txtConfidenceLimits.TextChanged += new TextChangedEventHandler(txtInputs_TextChanged);
            txtDesignEffect.TextChanged += new TextChangedEventHandler(txtInputs_TextChanged);
            txtNumberOfClusters.TextChanged += new TextChangedEventHandler(txtInputs_TextChanged);
			//EI-14
            txtPopulationSize.PreviewKeyDown += new KeyEventHandler(txtInput_PreviewKeyDown);
            txtExpectedFreq.PreviewKeyDown += new KeyEventHandler(txtInput_PreviewKeyDown);
            txtConfidenceLimits.PreviewKeyDown += new KeyEventHandler(txtInput_PreviewKeyDown);
            txtDesignEffect.PreviewKeyDown += new KeyEventHandler(txtInput_PreviewKeyDown);
            txtNumberOfClusters.PreviewKeyDown += new KeyEventHandler(txtInput_PreviewKeyDown);

            imgClose.MouseEnter += new MouseEventHandler(imgClose_MouseEnter);
            imgClose.MouseLeave += new MouseEventHandler(imgClose_MouseLeave);
            imgClose.MouseDown += new MouseButtonEventHandler(imgClose_MouseDown);
            mnuPrint.Click += new RoutedEventHandler(mnuPrint_Click);
            mnuSave.Click += new RoutedEventHandler(mnuSave_Click);

            txtPopulationSize.Text = "999999";
            txtExpectedFreq.Text = "50";
            txtConfidenceLimits.Text = "5";
            string separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            txtDesignEffect.Text = "1" + separator + "0";
            txtNumberOfClusters.Text = "1";
        }

        void mnuSave_Click(object sender, RoutedEventArgs e)
        {
            Common.SaveAsImage(pnlMainContent);
        }

        void mnuPrint_Click(object sender, RoutedEventArgs e)
        {
            Common.Print(pnlMainContent);
        }

        void imgClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (GadgetClosing != null)
                GadgetClosing(this);
        }

        void imgClose_MouseLeave(object sender, MouseEventArgs e)
        {
            Uri uriSource = new Uri("../Images/x.png", UriKind.Relative);
            imgClose.Source = new BitmapImage(uriSource);
        }

        void imgClose_MouseEnter(object sender, MouseEventArgs e)
        {
            Uri uriSource = new Uri("../Images/x_over.png", UriKind.Relative);
            imgClose.Source = new BitmapImage(uriSource);
        }

        void txtInputs_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!string.IsNullOrEmpty(txtPopulationSize.Text) && !string.IsNullOrEmpty(txtExpectedFreq.Text) && !string.IsNullOrEmpty(txtConfidenceLimits.Text) && !string.IsNullOrEmpty(txtDesignEffect.Text) && !string.IsNullOrEmpty(txtNumberOfClusters.Text))
            {
                int populationSize = 0;
                double expectedFreq = 0;
                double confidenceLimits = 0;
                double de = 1.0;
                int clusters = 1;
                bool parseResult1 = int.TryParse(txtPopulationSize.Text, out populationSize);
                bool parseResult2 = double.TryParse(txtExpectedFreq.Text, out expectedFreq);
                bool parseResult3 = double.TryParse(txtConfidenceLimits.Text, out confidenceLimits);
                bool parseResult4 = double.TryParse(txtDesignEffect.Text, out de);
                bool parseResult5 = int.TryParse(txtNumberOfClusters.Text, out clusters);

                if (parseResult1 && parseResult2 && parseResult3 && parseResult4 && parseResult5)
                {
                    int[] res = CalculateSampleSizes(populationSize, expectedFreq, confidenceLimits, de, clusters);
                    txt80.Text = res[0].ToString();
                    txt90.Text = res[1].ToString();
                    txt95.Text = res[2].ToString();
                    txt97.Text = res[3].ToString();
                    txt99.Text = res[4].ToString();
                    txt999.Text = res[5].ToString();
                    txt9999.Text = res[6].ToString();
                    ttxt80.Text = (clusters * res[0]).ToString();
                    ttxt90.Text = (clusters * res[1]).ToString();
                    ttxt95.Text = (clusters * res[2]).ToString();
                    ttxt97.Text = (clusters * res[3]).ToString();
                    ttxt99.Text = (clusters * res[4]).ToString();
                    ttxt999.Text = (clusters * res[5]).ToString();
                    ttxt9999.Text = (clusters * res[6]).ToString();
                }
            }
        }
		//EI-14
        protected virtual void txtInput_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool isNumPadNumeric = (e.Key >= Key.NumPad0 && e.Key <= Key.NumPad9) || e.Key == Key.Decimal;
            bool isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemPeriod;

            if (System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator == ",")
            {
                isNumeric = (e.Key >= Key.D0 && e.Key <= Key.D9) || e.Key == Key.OemComma;
            }

            if ((isNumeric || isNumPadNumeric) && Keyboard.Modifiers != ModifierKeys.None)
            {
                e.Handled = true;
                return;
            }
            bool isControl = ((Keyboard.Modifiers != ModifierKeys.None && Keyboard.Modifiers != ModifierKeys.Shift) || e.Key == Key.Back || e.Key == Key.Delete || e.Key == Key.Insert || e.Key == Key.Down || e.Key == Key.Left || e.Key == Key.Right || e.Key == Key.Up || e.Key == Key.Tab || e.Key == Key.PageDown || e.Key == Key.PageUp || e.Key == Key.Enter || e.Key == Key.Return || e.Key == Key.Escape || e.Key == Key.Home || e.Key == Key.End);
            e.Handled = !isControl && !isNumeric && !isNumPadNumeric;
        }

        #region IGadget Members

        public void RefreshResults()
        {
            
        }

        /// <summary>
        /// Updates the variable names available in the gadget's properties
        /// </summary>
        public void UpdateVariableNames()
        {
            //FillComboboxes(true);
        }

        public XmlNode Serialize(XmlDocument doc)
        {
            throw new NotImplementedException();
        }

        public void CreateFromXml(XmlElement element)
        {
        }

        /// <summary>
        /// Converts the gadget's output to Html
        /// </summary>
        /// <returns></returns>
        public string ToHTML(string htmlFileName = "", int count = 0, bool useAlternatingColors = false)
        {
            return string.Empty;

            //if (string.IsNullOrEmpty(txtPopulationSize.Text) || string.IsNullOrEmpty(txtExpectedFreq.Text) || string.IsNullOrEmpty(txtConfidenceLimits.Text))
            //{
            //    return string.Empty;
            //}

            //StringBuilder htmlBuilder = new StringBuilder();
            //Epi.Configuration config = Epi.Configuration.GetNewInstance();

            //htmlBuilder.AppendLine("<h2>StatCalc Population Survey</h2>");

            //htmlBuilder.AppendLine("<p><small>");
            //htmlBuilder.AppendLine("<strong>Population survey or descriptive study using random (not cluster) sampling</strong>");
            //htmlBuilder.AppendLine("<br />");
            //htmlBuilder.AppendLine("</small></p>");

            //htmlBuilder.AppendLine("<div style=\"height: 7px;\"></div>");
            //htmlBuilder.AppendLine("");
            //htmlBuilder.AppendLine("<table class=\"noborder\" border=\"0\" cellpadding=\"0\" cellspacing=\"0\">");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("<td class=\"noborder\" style=\"padding-right: 20px;\">");
            //htmlBuilder.AppendLine("");
            //htmlBuilder.AppendLine("");
            //htmlBuilder.AppendLine("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("<th>Confidence Level</th>");
            //htmlBuilder.AppendLine("<th>Sample Size</th>");
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr class=\"altcolor\">");
            //htmlBuilder.AppendLine("<td class=\"value\">Population size</td>");
            //htmlBuilder.AppendLine("<td>" + txtPopulationSize.Text + "</td>"); 
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("<td class=\"value\">Expected frequency</td>");
            //htmlBuilder.AppendLine("<td>" + txtExpectedFreq.Text + "</td>");
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr class=\"altcolor\">");
            //htmlBuilder.AppendLine("<td class=\"value\">Confidence limits</td>");
            //htmlBuilder.AppendLine("<td>" + txtConfidenceLimits.Text + "</td>");
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("</table>");
            //htmlBuilder.AppendLine("");
            //htmlBuilder.AppendLine("</td>");
            //htmlBuilder.AppendLine("<td class=\"noborder\">");
            //htmlBuilder.AppendLine("<table border=\"0\" cellpadding=\"0\" cellspacing=\"0\">");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("<th>Confidence Level</th>");
            //htmlBuilder.AppendLine("<th>Sample Size</th>");
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr class=\"altcolor\">");
            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">80%</td>");
            //htmlBuilder.AppendLine("<td>" + txt80.Text + "</td>");  
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">90%</td>");
            //htmlBuilder.AppendLine("<td>" + txt90.Text + "</td>");  
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr class=\"altcolor\">");
            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">95%</td>");
            //htmlBuilder.AppendLine("<td>" + txt95.Text + "</td>");  
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">97%</td>");
            //htmlBuilder.AppendLine("<td>" + txt97.Text + "</td>");  
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr class=\"altcolor\">");
            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">99%</td>");
            //htmlBuilder.AppendLine("<td>" + txt99.Text + "</td>");  
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr>");
            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">99.9%</td>");
            //htmlBuilder.AppendLine("<td>" + txt999.Text + "</td>");  
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("<tr class=\"altcolor\">");
            //htmlBuilder.AppendLine("<td style=\"text-align: center;\">99.99%</td>");
            //htmlBuilder.AppendLine("<td>" + txt9999.Text + "</td>");  
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("</table>");
            //htmlBuilder.AppendLine("</td>");
            //htmlBuilder.AppendLine("</tr>");
            //htmlBuilder.AppendLine("</table>");

            //return htmlBuilder.ToString();
        }

        public string CustomOutputHeading { get { return string.Empty; } set { } }
        public string CustomOutputDescription { get { return string.Empty; } set { } }
        public string CustomOutputCaption { get { return string.Empty; } set { } }
        #endregion

        public int[] CalculateSampleSizes(int pop, double freq, double worst, double de, int clusters)
        {
            double[] percentiles = new double[] { 0.80, 0.90, 0.95, 0.97, 0.99, 0.999, 0.9999 };
            int[] sizes = new int[7];
            //double d = Math.abs(freq - worst);
            double d = Math.Abs(worst);
            double factor = freq * (100 - freq) / (d * d);
            for (int i = 0; i < percentiles.Length; i++)
            {
                double twoTail = ANorm(1 - percentiles[i]);
                double n = twoTail * twoTail * factor;
                double sampleSize = n / (1 + (n / pop));
                sizes[i] = (int)Math.Round(sampleSize);
                sizes[i] = (int)Math.Ceiling(de * (double)sizes[i] / (double)clusters);
            }
            return sizes;
        }

        public void UnmatchedCaseControl(double a, double b, double vr, double v2, double vor, double v1)
        {
            double Za = ANorm(a);
            double Zb = 0;

            if (b >= 1)
            {
                b = b / 100.0;
            }
            if (b < 0.5)
            {
                Zb = -ANorm(2.0 * b);
            }
            else
            {
                Zb = ANorm(2.0 - 2.0 * b);
            }
            if (vor != 0)
            {
                v1 = v2 * vor / (1.0 + v2 * (vor - 1.0));
            }
            double pbar = (v1 + vr * v2) / (1.0 + vr);
            double qbar = 1.0 - pbar;
            double vn = ((Math.Pow((Za + Zb), 2.0)) * pbar * qbar * (vr + 1.0)) / ((Math.Pow((v1 - v2), 2.0)) * vr);
            double vn1 = Math.Pow(((Za * Math.Sqrt((vr + 1.0) * pbar * qbar)) + (Zb * Math.Sqrt((vr * v1 * (1.0 - v1)) + (v2 * (1.0 - v2))))), 2.0) / (vr * Math.Pow((v2 - v1), 2.0));
            double vn2 = Math.Pow(Za * Math.Sqrt((vr + 1.0) * pbar * qbar) + Zb * Math.Sqrt(vr * v1 * (1.0 - v1) + v2 * (1.0 - v2)), 2.0) / (vr * Math.Pow(Math.Abs(v1 - v2), 2.0));
            vn2 = vn2 * Math.Pow((1.0 + Math.Sqrt(1.0 + 2.0 * (vr + 1.0) / (vn2 * vr * Math.Abs(v2 - v1)))), 2.0) / 4.0;
        }

        public void Cohort(double a, double b, double vr, double v2, double vor, double v1, double rr, double dd)
        {
            double Za = ANorm(a);
            if (b >= 1)
            {
                b = b / 100.0;
            }
            double Zb;
            if (b < 0.5)
            {
                Zb = -ANorm(2.0 * b);
            }
            else
            {
                Zb = ANorm(2.0 - (2.0 * b));
            }
            if (vor != 0)
            {
                v1 = v2 * vor / (1.0 + v2 * (vor - 1.0));
            }
            double pbar = (v1 + vr * v2) / (1.0 + vr);
            double qbar = 1.0 - pbar;
            double vn = ((Math.Pow((Za + Zb), 2.0)) * pbar * qbar * (vr + 1.0)) / ((Math.Pow((v1 - v2), 2.0)) * vr);
            double vn1 = Math.Pow(((Za * Math.Sqrt((vr + 1.0) * pbar * qbar)) + (Zb * Math.Sqrt((vr * v1 * (1.0 - v1)) + (v2 * (1.0 - v2))))), 2.0) / (vr * Math.Pow((v2 - v1), 2.0));
            double vn2 = Math.Pow(Za * Math.Sqrt((vr + 1.0) * pbar * qbar) + Zb * Math.Sqrt(vr * v1 * (1.0 - v1) + v2 * (1.0 - v2)), 2.0) / (vr * Math.Pow(Math.Abs(v1 - v2), 2.0));
            vn2 = vn2 * Math.Pow((1.0 + Math.Sqrt(1.0 + 2.0 * (vr + 1.0) / (vn2 * vr * Math.Abs(v2 - v1)))), 2.0) / 4.0;
        }

        private double ANorm(double p)
        {
            double v = 0.5;
            double dv = 0.5;
            double z = 0;

            while (dv > 1e-6)
            {
                z = 1.0 / v - 1.0;
                dv = dv / 2.0;
                if (Norm(z) > p)
                {
                    v = v - dv;
                }
                else
                {
                    v = v + dv;
                }
            }

            return z;
        }

        private double Norm(double z)
        {
            z = Math.Sqrt(z * z);
            double p = 1.0 + z * (0.04986735 + z * (0.02114101 + z * (0.00327763 + z * (0.0000380036 + z * (0.0000488906 + z * 0.000005383)))));
            p = p * p; p = p * p; p = p * p;
            return 1.0 / (p * p);
        }

        private double OddsToPercentCases(double oddsRatio, double percentControls)
        {
            double rawVal = 0;
            if (oddsRatio != 0)
            {
                rawVal = 100 * percentControls * oddsRatio / (1 + percentControls * (oddsRatio - 1));
            }
            return Math.Round(100000 * rawVal) / 100000;
        }

        private double PercentCasesToOdds(double percentCases, double percentControls)
        {
            return Math.Round(100000 * (percentCases * (1 - percentControls)) / (percentControls * (1 - percentCases))) / 100000;
        }

        public void HideCloseIcon()
        {
            imgClose.Visibility = Visibility.Collapsed;
        }

        public int PreferredUIHeight
        {
            get { return 350; }
        }

        public int PreferredUIWidth
        {
            get { return 525; }
        }

        public bool IsProcessing
        {
            get
            {
                return false;
            }
            set
            {
                isProcessing = value;
            }
        }

        /// <summary>
        /// Sets the gadget to its 'processing' state
        /// </summary>
        public void SetGadgetToProcessingState()
        {
        }

        /// <summary>
        /// Sets the gadget to its 'finished' state
        /// </summary>
        public void SetGadgetToFinishedState()
        {
            if (GadgetProcessingFinished != null)
                GadgetProcessingFinished(this);
        }

        public bool DrawBorders { get; set; } 
    }
}
