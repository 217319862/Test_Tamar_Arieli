using System;
using System.Collections.Generic;
using System.Text;

namespace Test2
{
    internal class Laon
    {
        //reader_id,name,type,book,loan_day,return_day
        public string Reader_id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Book { get; set; }
        public int Loan_day { get; set; }
        public int Return_day { get; set; }

        public Laon(string reader_id, string name, string type, string book, int loan_day, int return_day)
        {
            this.Reader_id = reader_id;
            this.Name = name;
            this.Type = type;
            this.Book = book;
            this.Loan_day = loan_day;
            this.Return_day = return_day;
        }

        public override string ToString()
        {
            return $"Reader_id: {Reader_id} Name:{Name} Type: {Type} Book: {Book} Loan_day: {Loan_day} Return_day: {Return_day}";
        }
    }
}
