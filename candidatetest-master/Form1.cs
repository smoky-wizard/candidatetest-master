using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace candidatetest_master
{
    public partial class Form1 : Form
    {
        #region Variables -------------------------------

        /// <summary>
        /// Объект для записи данных
        /// </summary>
        public StreamWriter w;

        /// <summary>
        /// Объект для хранения данных из json-файла
        /// </summary>
        public string jsonData; 

        /// <summary>
        /// Объект для хранения данных из csv-файла
        /// </summary>
        List<Tuple<string, string, string>> csvData = new List<Tuple<string, string, string>>();

        /// <summary>
        /// Объект для формирования выходных данных
        /// </summary>
        List<Tuple<string, string>> listOut = new List<Tuple<string, string>>();

        #region Переменные для определения смещения
        string vDouble = "double";
        string vInt = "int";
        string vBool = "bool";
        #endregion

        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private void button_exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void button_file_Click(object sender, EventArgs e)
        {
            try
            {
                #region Считываем данные из файла CSV
                OpenFileDialog openFileDialog_csv = new OpenFileDialog();
                openFileDialog_csv.Multiselect = false;
                openFileDialog_csv.DefaultExt = "*.csv";
                openFileDialog_csv.Filter = "CSV Files (*.csv*)|*.csv*";
                openFileDialog_csv.Title = "Выберите файл с расширением CSV";
                openFileDialog_csv.ShowDialog();
                string xlFileName_csv = openFileDialog_csv.FileName; //имя файла csv

                using (StreamReader c = new StreamReader(xlFileName_csv))
                {
                    // Берем данные из файл csv и построчно будем записывать в наш список, пока не дойдем до конца файла
                    while(c.EndOfStream == false)
                    {
                        // Каждую строку разбиваем через разделитель и записываем в массив
                        string[] values = c.ReadLine().Split(';');
                        // теперь мы знаем что в какой колонке находится по такой структуре и записываем в наш список
                        csvData.Add(Tuple.Create(values[0],values[1],values[2]));
                    }
                    c.Close();
                }

                #endregion
                #region Считываем данные из файла JSON

                OpenFileDialog openFileDialog_json = new OpenFileDialog();
                openFileDialog_json.Multiselect = false;
                openFileDialog_json.DefaultExt = "*.json";
                openFileDialog_json.Filter = "JSON Files (*.json*)|*.json*";
                openFileDialog_json.Title = "Выберите файл с расширением JSON";
                openFileDialog_json.ShowDialog();
                string xlFileName_json = openFileDialog_json.FileName; //имя файла json

                using (StreamReader r = new StreamReader(xlFileName_json))
                {
                    // считываем всё содержимое файла json в строку
                    jsonData = r.ReadToEnd();
                    // отпускаем файл закрывая соединение
                    r.Close();
                }

                #endregion
                #region Создание xml-файла и выбор места куда его необходимо сохранить

                try
                {
                    // здесь будет запись в xml файл
                    MessageBox.Show("Укажите куда сохранить файл XML-файл");
                    SaveFileDialog fileDialog = new SaveFileDialog();
                    string filenameEx = "";

                    fileDialog.FileName = "result_candidatetest_" + DateTime.Now.ToString().Replace(":", "_").Replace(" ", "_") + ".xml";       // забиваем созданное имя в диалоговое окно
                    fileDialog.Filter = "xml files (*.xml)|*.xml|All files (*.*)|*.*";   // настраиваем фильтр расширения

                    if (fileDialog.ShowDialog() == DialogResult.OK)                // если окно открылось
                    {
                        filenameEx = fileDialog.FileName;                        // запоминаем название, как название документа
                        FileStream aFile = new FileStream(filenameEx, FileMode.OpenOrCreate);
                        w = new StreamWriter(aFile, Encoding.GetEncoding(1251)); // определяем объект для записи
                        aFile.Seek(0, SeekOrigin.End);

                        // Запускаем функцию для выполнения соединений по типу
                        Connection();
                    }
                }
                catch { MessageBox.Show("Место для сохранения файла XML не выбрано."); }

                #endregion
            }
            catch { }
        }

        private void Connection()
        {
            progressBar1.Maximum = csvData.Count; // максимально допустимое значение progressbara есть количество всеъ строк файла csv
            progressBar1.Value = 1; // начальное значение progressbara, 1 так как первую строку с названиями колонок не учитываем
         
            // циклом перебираем каждую строку файла csv на предмет совпадения типа с типов в файла json
            for (int i = 1; i < csvData.Count; i++)
            {
                int equal = 0; // флаг равенства типов если значение 0 - типы не равны, 1 - равны
                int address = 0; // счетчик адрес, при смене типа он сбрасывается
                #region Разбор JSON файла
                try
                {
                    dynamic items = JsonConvert.DeserializeObject(jsonData); // преобразовываем данные в структуру

                    foreach (var key in items)
                    {
                        foreach (var values in key)
                        {
                            foreach (var value in values)
                            {
                                // добрались до конкретной строки
                                foreach (var valueResult in value)
                                {
                                    // Берем тип для соответствия и сравниваем со типом из строки файла csv
                                    // Если типы совпадают, то меняем значение флага
                                    if (valueResult.Name.ToString() == "TypeName" && csvData[i].Item2 == valueResult.Value.ToString())
                                    {
                                        equal = 1; // флаг равенства типов
                                    }
                                    // Также в строке содержится и информация из области Propertys по определенному типу
                                    // Если мы уверены что типы совпадают и добрались до области Propertys, то работаем каждой строкой внутри нее
                                    if (valueResult.Name == "Propertys" && equal == 1)
                                    {
                                        foreach (var valueResult_one in valueResult)
                                        {
                                            foreach (var valueResult_two in valueResult_one)
                                            {
                                                // формируем строку названия для занесения по струкутре
                                                // название из файла csv + точка + приставка из файл json
                                                string val01 = csvData[i].Item1 + "." + valueResult_two.Name;

                                                // Для первого адреса элемента
                                                if (address == 0) 
                                                {
                                                    listOut.Add(Tuple.Create(val01, address.ToString())); // заносим в наш выходной список Название и Адрес
                                                    #region Определяем смещение
                                                    // Из теории мы знаем что тип данных double - 8 байт, int - 4 байт, bool - 1 байт
                                                    // Поэтому найдя подходящий предыдущий тип данных добавляем его к адресу
                                                    if (valueResult_two.Value.ToString() == vDouble)
                                                    {
                                                        address += 8;
                                                    }
                                                    if (valueResult_two.Value.ToString() == vInt)
                                                    {
                                                        address += 4;
                                                    }
                                                    if (valueResult_two.Value.ToString() == vBool)
                                                    {
                                                        address += 1;
                                                    }
                                                    #endregion
                                                }
                                                else // для последующих элементов
                                                {
                                                    listOut.Add(Tuple.Create(val01, address.ToString())); // 
                                                    #region Определяем смещение
                                                    // Из теории мы знаем что тип данных double - 8 байт, int - 4 байт, bool - 1 байт
                                                    // Поэтому найдя подходящий предыдущий тип данных добавляем его к адресу
                                                    if (valueResult_two.Value.ToString() == vDouble)
                                                    {
                                                        address += 8;
                                                    }
                                                    if (valueResult_two.Value.ToString() == vInt)
                                                    {
                                                        address += 4;
                                                    }
                                                    if (valueResult_two.Value.ToString() == vBool)
                                                    {
                                                        address += 1;
                                                    }
                                                    #endregion                                                    
                                                }
                                            }
                                        }
                                    }
                                }
                                // зануляем переменные, чтобы ничего лишнего в результат не занести
                                equal = 0; 
                                address = 0;
                            }
                        }
                    }
                }
                catch { }
                #endregion  
                progressBar1.Value++; // увеличиваем значение progressbara
            }
            WriteData(listOut); // функция для записи данных в xml-файл
            MessageBox.Show("Результат сформирован.");
            progressBar1.Value = 0;
        }

        private void WriteData(List<Tuple<string, string>> listOut)
        {
            // Возьмем общее количество строк в listOut и по очередно каждую строку будем записывать в файл по определенной структуре
            try
            {
                w.WriteLine("<root>");
                for (int i = 0; i < listOut.Count; i++)
                {
                    w.WriteLine("<item Binding=\"Introduced\">");
                    w.WriteLine("<node-path>{0}</node-path>", listOut[i].Item1);
                    w.WriteLine("<address>{0}</address>", listOut[i].Item2);
                    w.WriteLine("</item>");
                }
                w.WriteLine("</root>");
                w.Close(); // закрываем файл xml после завершения записи
            }
            catch { }
        }
    }
}
