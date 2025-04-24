#region Using declarations

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Gui;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Gui.SuperDom;
using NinjaTrader.Gui.Tools;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Core.FloatingPoint;
using NinjaTrader.NinjaScript.DrawingTools;
using NinjaTrader.NinjaScript.SuperDomColumns;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.IO;
using System.Net;
using System.Xml.Linq;
using System.Runtime.Remoting.Contexts;
using System.Windows.Media.TextFormatting;
using System.Windows.Markup;
using Infragistics.Windows.DataPresenter;
using NinjaTrader.NinjaScript.MarketAnalyzerColumns;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using NinjaTrader.Adapter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;
using System.Windows.Forms;
using NinjaTrader.NinjaScript.Indicators.LizardIndicators;

#endregion

namespace NinjaTrader.NinjaScript.Indicators
{
    [Gui.CategoryOrder("Buy/Sell Filters", 1)]
    [Gui.CategoryOrder("Advanced", 2)]
    [Gui.CategoryOrder("Colors", 3)]

    public class Optimus : Indicator
    {
        private string sVersion = "1.5";
        long iAsk, iBid;
        int iCurrBar;

        #region VARIABLES

        public struct lines
        {
            public string tag;
            public double loc;
        }

        List<lines> ll = new List<lines>();

        private bool sqRelaxUp = false;

        private SMA EMAS, EMAF;
        private Series<double> kama9, MACD1, sqzData, SqueezeDef, AO, atrSeries, bottomSeries, topSeries;

        private bool bBigArrowUp = false;
        private bool bDefibCalculated = false;
        private string PrevEvil = string.Empty;

        private double barHighValue, barLowValue, bottomValue, topValue;
        private int ATRMultiplier = 2;
        private int ATRPeriod = 11;

        private string sndBuy = NinjaTrader.Core.Globals.InstallDir + @"\sounds\Buy.wav";
        private string sndSell = NinjaTrader.Core.Globals.InstallDir + @"\sounds\Sell.wav";
        private string sndImb = NinjaTrader.Core.Globals.InstallDir + @"\sounds\Imbalance.wav";

        // --- NEW: SignalArrows series variable
        private Series<int> signalArrows;

        #endregion

        #region STATE CHANGES

        protected override void OnStateChange()
        {
            if (State == State.SetDefaults)
            {
                Description = @"Optimus Prime Indicator by TraderOracle";
                Name = "OptimusNinja";
                Calculate = Calculate.OnBarClose;
                IsOverlay = true;
                IsSuspendedWhileInactive = true;

                VI_Brush = Brushes.AliceBlue;
                Green_Brush = Brushes.Lime;
                Red_Brush = Brushes.Red;

                iMinADX = 0;
                iTextSize = 9;
                iDotSize = 12;
                iTickOffset = 7;
                sEmailAddress = "username@gmail.com";

                bLindaCandle = false;
                bWaddahCandle = false;
                iWaddahIntense = 160;
                iWaddahBuffer = 80;
                iLindaIntense = 40;

                bShowTramp = true;          
                bShowMACDPSARArrow = true;
                bShowRegularBuySell = true;
                bVolumeImbalances = true;
                bShowSqueeze = false;
                bShowRevPattern = true;
                bShowSquare = false;

                bUseFisher = true;          
                bUseWaddah = true;
                bUseT3 = true;
                bUsePSAR = true;
                bUseSuperTrend = true;
                bUseSqueeze = false;
                bUseMACD = false;
                bUseAO = false;
                bUseHMA = false;
                bShowKAMA9 = false;

                bPlaySounds = false;
                bSendEmail = false;
                bShowEvilTimes = false;
                myVersion = "(c) 2024 by TraderOracle, version " + sVersion;

                AddPlot(Brushes.DarkOrange, "kama9");
            }
            else if (State == State.Configure)
            {
                MACD1 = new Series<double>(this);
                sqzData = new Series<double>(this);
                SqueezeDef = new Series<double>(this);
                AO = new Series<double>(this);
                kama9 = new Series<double>(this);
                AddVolumetric(Instrument.FullName, BarsPeriod.BarsPeriodType, BarsPeriod.Value, VolumetricDeltaType.BidAsk, 1);
            }
            else if (State == State.DataLoaded)
            {
                EMAF = SMA(3);
                EMAS = SMA(10);
                topSeries = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
                bottomSeries = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);
                atrSeries = new Series<double>(this, MaximumBarsLookBack.TwoHundredFiftySix);

                // --- NEW: Initialize SignalArrows series
                signalArrows = new Series<int>(this);
            }
        }

        #endregion

        protected override void OnMarketData(MarketDataEventArgs marketDataUpdate)
        {
            // ... unchanged ...
        }

        protected override void OnBarUpdate()
        {
            // --- NEW: Default to no signal every bar
            signalArrows[0] = 0;

            // ... (existing indicator logic/calculations) ...

            // ========================    UP CONDITIONS    ===========================
            if (green && bShowUp && bShowRegularBuySell)
            {
                DrawText("▴", Green_Brush, false, true);
                if (bPlaySounds)
                    Alert("Alert", Priority.Medium, "Standard BUY", sndBuy, 10, Brushes.Black, Brushes.BlanchedAlmond);
                if (bSendEmail)
                    SendMail(sEmailAddress, "Standard BUY", "Standard BUY " + Instrument + " " + Close[0].ToString());
                // --- NEW: Set buy signal
                signalArrows[0] = 1;
            }

            // ========================    DOWN CONDITIONS    =========================
            if (red && bShowDown && bShowRegularBuySell)
            {
                DrawText("▾", Red_Brush, false, true);
                if (bPlaySounds)
                    Alert("Alert", Priority.Medium, "Standard SELL", sndSell, 10, Brushes.Black, Brushes.BlanchedAlmond);
                if (bSendEmail)
                    SendMail(sEmailAddress, "Standard SELL", "Standard SELL " + Instrument + " " + Close[0].ToString());
                // --- NEW: Set sell signal
                signalArrows[0] = -1;
            }

            // ... (rest of OnBarUpdate, unchanged) ...
        }

        #region MISC FUNCTIONS
        // ... (unchanged: Defibillator, DrawText, EvilTimes, etc.) ...
        #endregion

        #region Parameters

        [NinjaScriptProperty]
        [Display(Name = "Waddah Explosion", GroupName = "Buy/Sell Filters", Order = 1)]
        public bool bUseWaddah { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Awesome Oscillator", GroupName = "Buy/Sell Filters", Order = 2)]
        public bool bUseAO { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Parabolic SAR", GroupName = "Buy/Sell Filters", Order = 3)]
        public bool bUsePSAR { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Squeeze Momentum", GroupName = "Buy/Sell Filters", Order = 4)]
        public bool bUseSqueeze { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Linda MACD", GroupName = "Buy/Sell Filters", Order = 5)]
        public bool bUseMACD { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Hull Moving Avg", GroupName = "Buy/Sell Filters", Order = 6)]
        public bool bUseHMA { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "SuperTrend", GroupName = "Buy/Sell Filters", Order = 7)]
        public bool bUseSuperTrend { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "T3", GroupName = "Buy/Sell Filters", Order = 8)]
        public bool bUseT3 { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Fisher Transform", GroupName = "Buy/Sell Filters", Order = 9)]
        public bool bUseFisher { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Minimum ADX", GroupName = "Buy/Sell Filters", Order = 10)]
        public int iMinADX { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Use Lizard ADXVMA", GroupName = "Buy/Sell Filters", Order = 11)]
        public bool lizADXVMA { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Use Lizard SuperTrend", GroupName = "Buy/Sell Filters", Order = 12)]
        public bool lizSUPER { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Use Lizard MultiTSI", GroupName = "Buy/Sell Filters", Order = 13)]
        public bool lizMultiTSI { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Regular Buy/Sell Arrow", GroupName = "Advanced", Order = 1)]
        public bool bShowRegularBuySell { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show MACD/PSAR Big Arrow", GroupName = "Advanced", Order = 2)]
        public bool bShowMACDPSARArrow { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show Volume Imbalances", GroupName = "Advanced", Order = 3)]
        public bool bVolumeImbalances { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show Trampoline", GroupName = "Advanced", Order = 4)]
        public bool bShowTramp { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show Squeeze Relaxer", GroupName = "Advanced", Order = 5)]
        public bool bShowSqueeze { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show Reversal Patterns", GroupName = "Advanced", Order = 6)]
        public bool bShowRevPattern { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show Reversal Square", GroupName = "Advanced", Order = 7)]
        public bool bShowSquare { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show Advanced Signals", GroupName = "Advanced", Order = 8)]
        public bool bShowAdvanced { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show FighterOfEvilTimes", GroupName = "Advanced", Order = 9)]
        public bool bShowEvilTimes { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Play Alert Sounds", GroupName = "Advanced", Order = 10)]
        public bool bPlaySounds { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Send Email Alerts", GroupName = "Advanced", Order = 11)]
        public bool bSendEmail { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Email Address", GroupName = "Advanced", Order = 12)]
        public string sEmailAddress { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Alert Text Size", GroupName = "Advanced", Order = 13)]
        public int iTextSize { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Dots and Arrow Size", GroupName = "Advanced", Order = 14)]
        public int iDotSize { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Indicator tick offset", GroupName = "Advanced", Order = 15)]
        public int iTickOffset { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show KAMA 9", GroupName = "Advanced", Order = 16)]
        public bool bShowKAMA9 { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Indicator Version", GroupName = "Advanced", Order = 17)]
        public string myVersion { get; set; }

        [NinjaScriptProperty]
        [Display(Name = "Show Waddah Candles", GroupName = "Colored Candles", Order = 1)]
        public bool bWaddahCandle { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Waddah Intensity", GroupName = "Colored Candles", Order = 2)]
        public int iWaddahIntense { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Waddah Buffer", GroupName = "Colored Candles", Order = 3)]
        public int iWaddahBuffer { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Show Linda MACD Candles", GroupName = "Colored Candles", Order = 4)]
        public bool bLindaCandle { get; set; }
        [NinjaScriptProperty]
        [Display(Name = "Linda MACD Intensity", GroupName = "Colored Candles", Order = 5)]
        public int iLindaIntense { get; set; }

        [XmlIgnore]
        [Display(Name = "Volume Imbalance Color", GroupName = "Colors", Order = 1)]
        public Brush VI_Brush { get; set; }
        [XmlIgnore]
        [Display(Name = "Green Global Color", GroupName = "Colors", Order = 2)]
        public Brush Green_Brush { get; set; }
        [XmlIgnore]
        [Display(Name = "Red Global Color", GroupName = "Colors", Order = 3)]
        public Brush Red_Brush { get; set; }

        #endregion

        // --- NEW: Expose SignalArrows for strategies
        [Browsable(false)]
        [XmlIgnore()]
        public Series<int> SignalArrows
        {
            get { return signalArrows; }
        }

    }
}
