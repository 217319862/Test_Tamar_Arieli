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
        //public List<Laon> FillTheLoanListFromData(string[] lines)
        //{
        //    List<Laons> list = new List<Laons> { };
        //    for (int i = 2; i < lines.Length; i++)
        //    { 
        //        string[] s = lines[i].Split(',');
        //        string _Reader_id = s[0];
        //        string _Name = s[1];
        //        string _type = s[2];
        //        string _Book = s[3];
        //        int _loan_day =int.Parse( s[4]);
        //        int _return_day;
        //        if (s[5] == "")
        //             _return_day = 0;
        //        else 
        //             _return_day =int.Parse(s[5]);
        //        Laon laon=new Laon(_Reader_id,_Name,_type, _Book, _loan_day,_return_day);
        //        Console.WriteLine(laon.Reader_id);
        //        Console.WriteLine(laon.Name);
        //        list.Add(laon);
               
        //    }
        //    Console.WriteLine(list.Count);
        //    return list;
        //}
    }

}