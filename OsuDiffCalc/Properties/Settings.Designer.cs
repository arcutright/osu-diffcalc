﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace OsuDiffCalc.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.0.3.0")]
    public sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("600")]
        public int AutoUpdateIntervalNormalMs {
            get {
                return ((int)(this["AutoUpdateIntervalNormalMs"]));
            }
            set {
                this["AutoUpdateIntervalNormalMs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("600")]
        public int AutoUpdateIntervalMinimizedMs {
            get {
                return ((int)(this["AutoUpdateIntervalMinimizedMs"]));
            }
            set {
                this["AutoUpdateIntervalMinimizedMs"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("3.5")]
        public double FamiliarStarTargetMinimum {
            get {
                return ((double)(this["FamiliarStarTargetMinimum"]));
            }
            set {
                this["FamiliarStarTargetMinimum"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("7.5")]
        public double FamiliarStarTargetMaximum {
            get {
                return ((double)(this["FamiliarStarTargetMaximum"]));
            }
            set {
                this["FamiliarStarTargetMaximum"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool ShowFamiliarStarRating {
            get {
                return ((bool)(this["ShowFamiliarStarRating"]));
            }
            set {
                this["ShowFamiliarStarRating"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool EnableXmlCache {
            get {
                return ((bool)(this["EnableXmlCache"]));
            }
            set {
                this["EnableXmlCache"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool AlwaysOnTop {
            get {
                return ((bool)(this["AlwaysOnTop"]));
            }
            set {
                this["AlwaysOnTop"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool EnableAutoBeatmapAnalyzer {
            get {
                return ((bool)(this["EnableAutoBeatmapAnalyzer"]));
            }
            set {
                this["EnableAutoBeatmapAnalyzer"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("StackedColumn")]
        public global::System.Windows.Forms.DataVisualization.Charting.SeriesChartType SeriesChartType {
            get {
                return ((global::System.Windows.Forms.DataVisualization.Charting.SeriesChartType)(this["SeriesChartType"]));
            }
            set {
                this["SeriesChartType"] = value;
            }
        }
    }
}
