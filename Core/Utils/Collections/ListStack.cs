﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Yaw.Core.Utils.Collections
{
    /// <summary>
    /// Коллекция, которая совмещает в себе функциональность List-а и Stack-а
    /// </summary>
    /// <remarks>
    /// Вершиной стека считается последний элемент в списке
    /// </remarks>
    [Serializable]
    public class ListStack<T> : List<T> where T : class
    {
        /// <summary>
        /// Индекс элемента, который находится на вершине стека.
        /// Если стек пуст, то -1
        /// </summary>
        public int TopIndex
        {
            get
            {
                return Count - 1;
            }
        }

        /// <summary>
        /// Возвращает элемент, который находится на вершине стека без его удаления
        /// </summary>
        /// <exception cref="System.InvalidOperationException">если стек пуст</exception>
        /// <returns>элемент на вершине стека</returns>
        public T Peek()
        {
            if (Count == 0)
                throw new InvalidOperationException("Стек пуст");

            return base[TopIndex];
        }

        /// <summary>
        /// Возвращает элемент, который находится на вершине стека и удаляет его с вершины стека
        /// </summary>
        /// <exception cref="System.InvalidOperationException">если стек пуст</exception>
        /// <returns>элемент на вершине стека</returns>
        public T Pop()
        {
            if (Count == 0)
                throw new InvalidOperationException("Стек пуст");

            var top = base[TopIndex];
            RemoveAt(TopIndex);

            return top;
        }

        /// <summary>
        /// Добавляет новый элемент в стек - кладет его на вершину стека
        /// </summary>
        /// <param name="item">новый элемент стека</param>
        public void Push(T item)
        {
            CodeContract.Requires(item != null);
            Add(item);
        }

        /// <summary>
        /// Возвращает строку с перечисленными элементами стека (вершина - последняя)
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            var sb = new StringBuilder(Count * 20);
            foreach (var item in this)
            {
                sb.Append(item);
                sb.Append(',');
            }

            if (sb.Length > 0)
                sb.Length -= 1;

            return sb.ToString();
        }
    }
}
