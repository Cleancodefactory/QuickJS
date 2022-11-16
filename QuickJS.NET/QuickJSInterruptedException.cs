namespace QuickJS
{
	/// <summary>
	/// The exception that is thrown when the execution of JS code is interrupted.
	/// </summary>
	public class QuickJSInterruptedException : QuickJSException
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QuickJSInterruptedException"/>.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public QuickJSInterruptedException(string message)
			: base(message)
		{

		}

	}
}
