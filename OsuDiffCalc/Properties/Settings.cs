namespace OsuDiffCalc.Properties {
	using System.Configuration;


	// This class allows you to handle specific events on the settings class:
	//  The SettingChanging event is raised before a setting's value is changed.
	//  The PropertyChanged event is raised after a setting's value is changed.
	//  The SettingsLoaded event is raised after the setting values are loaded.
	//  The SettingsSaving event is raised before the setting values are saved.
	[SettingsProvider(typeof(PortableSettingsProvider))]
	public sealed partial class Settings {
		private readonly SettingsProvider Provider = new PortableSettingsProvider();

		public Settings() {
			// Try to re-use an existing provider, since we cannot have multiple providers
			// with same name.
			if (Providers[Provider.Name] is null) {
				Providers.Clear();
				Providers.Add(Provider);
			}
			else
				Provider = Providers[Provider.Name];

			// Change default provider.
			foreach (SettingsProperty property in Properties) {
				if (property.PropertyType.GetCustomAttributes(typeof(SettingsProviderAttribute), false).Length == 0) {
					property.Provider = Provider;
				}
			}

			// // To add event handlers for saving and changing settings, uncomment the lines below:
			//
			// this.SettingChanging += this.SettingChangingEventHandler;
			//
			// this.SettingsSaving += this.SettingsSavingEventHandler;
			//
		}

		private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
			// Add code to handle the SettingChangingEvent event here.
		}

		private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
			// Add code to handle the SettingsSaving event here.
		}
	}
}
