namespace Demo.App
{
	internal class Program
	{
		static void Main(string[] args)
		{
			var order = Order.CreateSample();

			using (var ms = new MemoryStream())
			{
				order.SerializeTo(ms);
				Console.WriteLine("Serialized " + ms.Length + " bytes");
			}
		}
	}
}
