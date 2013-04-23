using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyMovies
{
    public class SelectBuilder
    {
        string _fromTable;
        SelectBuilderFields _fields = new SelectBuilderFields();
        SelectBuilderJoins _joins = new SelectBuilderJoins();
        SelectBuilderWhere _where = new SelectBuilderWhere();
        SelectBuilderOrder _order = new SelectBuilderOrder();

        public SelectBuilder(string fromTable)
        {
            _fromTable = fromTable;
            Distinct = false;
        }

        public bool Distinct { get; set; }

        public SelectBuilderFields Fields
        {
            get { return _fields; }
        }

        public SelectBuilderJoins Joins
        {
            get { return _joins; }
        }

        public SelectBuilderWhere Where
        {
            get { return _where; }
        }

        public SelectBuilderOrder Order
        {
            get { return _order; }
        }

        public override string ToString()
        {
            return (Fields.Exists) ? string.Format("SELECT {0}{1} FROM {2}{3}{4}{5}", Distinct ? "DISTINCT " : "", _fields.ToString(), _fromTable, _joins.ToString(), _where.ToString(), _order.ToString()) : "";
        }
    }

    public abstract class SelectBuilderBase
    {
        protected StringBuilder _data = new StringBuilder();

        public SelectBuilderBase()
        {
        }

        public abstract void Add(string field);

        public bool Exists
        {
            get { return (_data.Length > 0); }
        }

        public override string ToString()
        {
            if (Exists)
            {
                return string.Concat(" ", _data.ToString());
            }
            return _data.ToString();
        }

        protected void Append(string suffix)
        {
            if (suffix.Length > 0)
            {
                _data.AppendFormat(" {0}", suffix);
            }
        }
    }

    public class SelectBuilderFields : SelectBuilderBase
    {
        public override void Add(string field)
        {
            if (field.Length > 0)
            {
                _data.AppendFormat("{0}{1}", (Exists) ? "," : "", field);
            }
        }
    }

    public class SelectBuilderJoins : SelectBuilderBase
    {
        public enum Types
        { 
            INNER,
            LEFT,
            FULL,
            CROSS
        }

        public override void Add(string joinTable)
        {
            Add(joinTable, Types.INNER);
        }
        public void Add(string joinTable, Types joinType)
        {
            if (joinTable.Length > 0)
            {
                _data.AppendFormat("{0}{1}", Join(joinType), joinTable);
            }
        }

        private string Join(Types joinType)
        {
            switch (joinType)
            {
                case Types.LEFT:
                    return " LEFT OUTER JOIN ";
                case Types.FULL:
                    return " FULL OUTER JOIN ";
                case Types.CROSS:
                    return " CROSS JOIN ";
                default:
                    return " INNER JOIN ";
            }
        }
    }

    public class SelectBuilderWhere : SelectBuilderBase
    {
        public override void Add(string field)
        {
            if (field.Length > 0)
            {
                _data.AppendFormat("{0}{1}", (!Exists) ? "WHERE " : " AND ", field);
            }
        }
    }

    public class SelectBuilderOrder : SelectBuilderBase
    {
        public SelectBuilderOrder()
        {
            Ascending = false;          // Default to descending
        }

        public override void Add(string field)
        {
            if (field.Length > 0)
            {
                _data.AppendFormat("{0}{1}", (!Exists) ? "Order By " : ",", field);
            }
        }

        public bool Ascending { get; set; }

        public override string ToString()
        {
            if (Exists)
            {
                base.Append((Ascending) ? "ASC" : "DESC");
            }
            return base.ToString();
        }
    }

}
