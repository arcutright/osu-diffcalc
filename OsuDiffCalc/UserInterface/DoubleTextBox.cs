namespace OsuDiffCalc.UserInterface {
	using System.Globalization;

	/// <summary>
	/// Validating TextBox which only accepts valid <see cref="double"/> input
	/// </summary>
	public class DoubleTextBox : NumericTextBox<double> {
		protected override void OnTextValidating(object sender, TextValidatingEventArgs e) {
			e.IsValid = double.TryParse(e.NewText, _numberStyle, CultureInfo.CurrentUICulture, out var value);
			if (e.IsValid)
				Value = value;
		}
	}
}
