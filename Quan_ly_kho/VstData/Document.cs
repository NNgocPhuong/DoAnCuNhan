using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace System
{
    public class DocumentMap<T> : Dictionary<string, T>
        where T: Document
    {
        new public T this[string objectId]
        {
            get
            {
                if (string.IsNullOrEmpty(objectId))
                {
                    return default(T);
                }

                T value;
                TryGetValue(objectId, out value);

                return value;
            }
            set
            {
                if (base.ContainsKey(objectId))
                {
                    base[objectId] = value;
                }
                else
                {
                    base.Add(objectId, value);
                }
            }
        }
        public void Add(T doc)
        {
            this[doc.ObjectId] = doc;
        }
        public void AddRange(IEnumerable<T> items)
        {
            foreach (var doc in items)
            {
                this.Add(doc);
            }
        }
        new public DocumentMap<T> Clear()
        {
            base.Clear();
            return this;
        }
    }
    public class DocumentList : List<Document>
    {
        public DocumentList() { }
        public DocumentList(IEnumerable<Document> items)
        {
            this.AddRange(items);
        }
        public Document Push(object value)
        {
            
            Document doc = value as Document;
            if (doc == null)
            {
                doc = Document.FromObject(value);
            }
            base.Add(doc);

            return doc;
        }
        //public IEnumerable<Document> Abc() => this.OrderBy(x => x.Ten);

        public DocumentGroup[] GroupBy(params string[] names)
        {
            var map = new Dictionary<string, DocumentGroup>();
            foreach (var doc in this)
            {
                DocumentGroup ext;
                var key = doc.Unique(names);

                if (!map.TryGetValue(key, out ext))
                {
                    map.Add(key, ext = new DocumentGroup());
                    ext.Copy(doc, names);
                }
                ext.Groups.Add(doc);
            }
            return map.Values.ToArray();
        }
    }
    public class DocumentMap : DocumentMap<System.Document> { }
    public class DocumentGroup : Document
    {
        DocumentList groups;
        public DocumentList Groups
        {
            get
            {
                if (groups == null)
                {
                    Push("items", groups = new DocumentList());
                }
                return groups;
            }
        }
    }
}
