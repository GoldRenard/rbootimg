using System;

namespace rbootimg.Utils
{
  /// <summary>
  /// Класс вспомогательных функций консоли
  /// </summary>
  public static class ConsoleEx
  {
    /// <summary>
    /// Вывод в консоль текста с указанным цветом без перевода строки
    /// </summary>
    /// <param name="ForegroundColor">Цвет текста</param>
    /// <param name="Msg">Сообщение</param>
    /// <param name="args">Параметры</param>
    public static void Write(ConsoleColor ForegroundColor, string Msg, params object[] args)
    {
      ConsoleColor OldColor = Console.ForegroundColor;
      Console.ForegroundColor = ForegroundColor;
      Console.Write(Msg, args);
      Console.ForegroundColor = OldColor;
    }

    /// <summary>
    /// Вывод в консоль текста с указанным цветом с переводом строки
    /// </summary>
    /// <param name="ForegroundColor">Цвет текста</param>
    /// <param name="Msg">Сообщение</param>
    /// <param name="args">Параметры</param>
    public static void WriteLine(ConsoleColor ForegroundColor, string Msg, params object[] args)
    {
      Write(ForegroundColor, Msg, args);
      Console.WriteLine();
    }
  }
}
