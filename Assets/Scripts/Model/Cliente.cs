public class Cliente
{
  public int id;
  public string color;

  public Cliente(int id, string color)
  {
    this.id = id;
    this.color = color;
  }

  public override string ToString()
  {
    return "Cliente #" + id + " (" + color + ")";
  }
}