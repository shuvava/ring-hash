using System;
using System.Collections.Generic;
using System.Data;


namespace common
{
    public static class DataTableUtill
    {
        /// <summary>
        ///     Map object to DataTable
        /// </summary>
        /// <typeparam name="T">Any class</typeparam>
        /// <param name="items">List of items</param>
        /// <param name="dataTableName">name of user define type</param>
        /// <returns>DataTable object</returns>
        internal static DataTable MapListToDataTable<T>(IEnumerable<T> items, string dataTableName = null)
        {
            var result = string.IsNullOrEmpty(dataTableName) ? new DataTable() : new DataTable(dataTableName);
            var hash = new HashSet<string>();
            var objType = Activator.CreateInstance<T>().GetType();

            foreach (var prop in objType.GetProperties())
            {
                result.Columns.Add(prop.Name, prop.PropertyType);
                hash.Add(prop.Name);
            }

            foreach (var item in items)
            {
                var row = result.NewRow();

                foreach (var prop in hash)
                {
                    var value = objType.GetProperty(prop)?.GetValue(item);
                    row[prop] = value ?? DBNull.Value;
                }

                result.Rows.Add(row);
            }

            return result;
        }
    }
}
