using System;
using System.Collections.Generic;
using System.Text;

namespace RobotSolver
{
  class Program
  {
    static void Main(string[] args)
    {
      new Solver().Go();
    }
  }

  /// <summary>
  /// Solves the light-bot game
  /// </summary>
  class Solver
  {
    // The commands the robot can do
    enum Cmd { Nothing, Call1, Call2, Jump, Left, Right, Light, Forwards };

    // ****** Level 10 \/ \/ \/
    string[] grid =
    {
    // 01234567   <- x
      "        ", // y=0
      "        ", //   1
      "        ", //   2
      "        ", //   3
      "   43422", //   4
      "2224232 ", //   5
      "    1   ", //   6
      "        "  //   7
    };
    int l1x = 0, l1y = 5;
    int l2x = 7, l2y = 4;
    int startrx = 0, startry = 6, startra = 1;  // facing E on the grid
    // ****** Level 10 /\ /\ /\

    bool l1, l2; // light 1, light 2
    int rx, ry, ra; // current robot x,y,angle
    Int64 tries = 0; // tries counter

    string Directions(Cmd[] list)
    {
      string s = "";
      for (int i = 0; i < 28; i++)
        s += list[i].ToString() + (i==11 || i==19 || i==27 ? "\n" : ",");
      return s;
    }

    void Reset()
    {
      l1 = false;
      l2 = false;
      rx = startrx;
      ry = startry;
      ra = startra;
    }

    int Height()
    {
      if (' ' == grid[ry][rx]) return 0;
      return grid[ry][rx] - '0';
    }

    void Clip()
    {
      if (rx < 0) rx = 0;
      if (rx > 7) rx = 7;
      if (ry < 0) ry = 0;
      if (ry > 7) ry = 7;
    }

    void Move()
    {
      if (ra == 0) ry--; // N
      if (ra == 1) rx++; // E
      if (ra == 2) ry++; // S
      if (ra == 3) rx--; // W
    }

    void Exec(Cmd c)
    {
      int oldx = rx;
      int oldy = ry;
      int oldht = Height();
      if (c == Cmd.Forwards)
      {
        Move();
        Clip();
        // can't climb/fall
        if (oldht != Height())
        {
          rx = oldx;
          ry = oldy;
        }
      }
      if (c == Cmd.Jump)
      {
        Move();
        Clip();
        // must climb one level or fall any levels
        int newht = Height();
        if (!(newht == oldht + 1 || newht < oldht))
        {
          rx = oldx;
          ry = oldy;
        }
      }
      if (c == Cmd.Left)
      {
        ra--;
        if (ra < 0) ra = 3;
      }
      if (c == Cmd.Right)
      {
        ra++;
        if (ra > 3) ra = 0;
      }
      if (c == Cmd.Light)
      {
        if (l1x == rx && l1y == ry) l1 = !l1;
        if (l2x == rx && l2y == ry) l2 = !l2;
      }
    }

    void TryPath(Cmd[] f)
    {
      tries++;
      Reset();

      int index = 0;
      List<int> stacki = new List<int>();
      bool done = false;
      do
      {
        Cmd c = f[index];

        // call other functions
        if (c == Cmd.Call1)
        {
          stacki.Add(index);
          index = 12;
        }
        if (c == Cmd.Call2)
        {
          stacki.Add(index);
          index = 20;
        }
        if (c != Cmd.Call1 && c != Cmd.Call2)
        {
          // run it
          Exec(c);

          // move next
          index++;

          while (index == 20 || index == 28) // finished f1 or f2
          {
            // pop
            index = stacki[stacki.Count - 1] + 1;
            stacki.RemoveAt(stacki.Count - 1);
          }
          if (index == 12) done = true; // did it just end?
        }
      } while (!done && stacki.Count < 10);

      if (stacki.Count >= 10) l1 = l2 = false; // if it looped then 
    }

    public void Go()
    {
      Console.WriteLine("Begin: " + DateTime.Now.ToString());

      Random r = new Random();
      int update_counter = 0;
      bool done = false;
      int i, j, morphs, successrate, morphcount, freshrate, morphrate, spreadrate;
      Cmd[] morphgood = new Cmd[28];

      Cmd[][] f;
      f = new Cmd[1000][]; // 1000 members of this generation
      for (j = 0; j < 1000; j++) f[j] = new Cmd[28];

      // Start with a random generation
      for (j = 0; j < 1000; j++)
      {
        for (i = 0; i < 27; i++)
        {
          f[j][i] = (Cmd)r.Next(8);
        }
      }

      bool[] goodones = new bool[1000]; // which ones in that generation were any good

      do
      { // Loop through a single generation

        // Go through all members of this generation
        successrate = 0;
        for (j = 0; j < 1000; j++)
        {
          // try it
          TryPath(f[j]);

          goodones[j] = (l1 || l2); // is this one any good?

          if (l1 || l2) // It's a half winner
          {
            successrate++;
          }

          if (l1 && l2) // Found a winner!
          {
            done = true;
            Console.WriteLine("\nWINNER:\n{0}", Directions(f[j]));
          }
        }

        // Randomise the generation
        morphcount = 0;
        freshrate = 0;
        morphrate = 0;
        spreadrate = 0;
        for (j = 0; j < 1000; j++)
        {
          if (goodones[j])
          { // If it's half good, then morph it
            // before i morph it, copy it for later morphing
            Array.Copy(f[j], morphgood, 28);

            morphs = r.Next(4); // how many fields to morph
            for (i = 0; i < morphs; i++) // morph X entries
            {
              f[j][r.Next(28)] = (Cmd)r.Next(8);
            }
            morphrate++;
            morphcount = 10; // morph this one over the next 10 crap ones
          }
          else // If its crap, then overwrite it
          {
            // Shall i spread and morph a previous half-good one into this slot?
            if (morphcount > 0)
            {
              Array.Copy(morphgood, f[j], 28); // copy it into this slot
              morphs = r.Next(4); // how many fields to morph
              for (i = 0; i < morphs; i++) // morph X entries
              {
                f[j][r.Next(28)] = (Cmd)r.Next(8);
              }
              morphcount--;
              spreadrate++;
            }
            else
            { // just totally regenerate it
              for (i = 0; i < 27; i++)
              {
                f[j][i] = (Cmd)r.Next(8);
              }
              freshrate++;
            }
          }
        }

        // Update the user
        update_counter++;
        if (update_counter >= 100)
        {
          update_counter = 0;
          Console.Write("\rTries: {0:#,0} Halfway: {1} Fresh: {2} Morphed: {3} Spread: {4} ", tries, successrate, freshrate, morphrate, spreadrate);
        }

      } while (!done);

      Console.WriteLine("End: " + DateTime.Now.ToString());
      Console.WriteLine("Press enter");
      Console.ReadLine();
    }
  }
}
