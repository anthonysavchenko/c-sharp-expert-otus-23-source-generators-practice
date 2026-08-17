using Demo.Generators;

namespace Demo.App
{
  [GenerateSerializer]
  public partial class Order
  {
    public int Id { get; set; }
    public string Symbol { get; set; }
    public double Price { get; set; }
    public bool IsActive { get; set; }

    public Order()
    {
      Symbol = string.Empty;
    }

    public static Order CreateSample()
    {
      return new Order
      {
        Id = 10,
        Price = 125.50,
        Symbol = "AAPL",
        IsActive = true,
      };
    }
  }
}