using System;
using System.Collections.Generic;
using System.Windows;

namespace datagridscroll
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            List<Book> books = new List<Book>();

            books.Add(new Book() { Id = 1, Title = "book1", Author = "aut\nhor" });
            books.Add(new Book() { Id = 2, Title = "book2", Author = "authoraaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa" });
            books.Add(new Book() { Id = 3, Title = "bo\nok3", Author = "author" });

            dataGrid01.ItemsSource = books;
            dataGrid02.ItemsSource = books;
        }

        public class Book
        {
            public int Id { get; set; }
            public string Title { get; set; }
            public string Author { get; set; }
        }
    }
}
