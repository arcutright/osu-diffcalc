namespace OsuDiffCalc.UserInterface {
	using System.Globalization;

	/// <summary>
	/// Validating TextBox which only accepts valid <see cref="int"/> input
	/// </summary>
	public class IntTextBox : NumericTextBox<int> {
		protected override void OnTextValidating(object sender, TextValidatingEventArgs e) {
			e.IsValid = int.TryParse(e.NewText, _numberStyle, CultureInfo.CurrentUICulture, out var value);
			if (e.IsValid)
				Value = value;
		}
	}
}
