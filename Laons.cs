using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Test2
{
    public enum Type
    {
        Student,
        Teachet,
        Guast
    }
    internal class Laons
    {
        public List<Laon> FillTheLoanListFromData(List<string[]> rows)
        {
            System.Diagnostics.Debug.WriteLine("NEW VERSION");
            List<Laon> list = new List<Laon> { };
            for (int i = 0; i < rows.Count; i++)
            {
                string readerId = rows[i][0];
                string name = rows[i][1];
                string type = rows[i][2];
                string book = rows[i][3];

                string loanDayText = rows[i][4];
                string returnDayText = rows[i][5];
                for (int column = 0; column < rows[i].Length; column++)
                {
                    Console.WriteLine($"{column}: '{rows[i][column]}'");
                }
                Laon laon = new Laon(readerId, name, type, book, int.Parse(rows[i][4]), int.Parse(rows[i][5]));
                list.Add(laon);

                Console.WriteLine(list.Count);

            }
            return list;
        }
    }
}
    
          
        
    

