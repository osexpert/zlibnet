using System;
using System.Collections;

namespace ZLibNet
{
    
    /// <summary>A collection that stores <see cref='OrganicBit.Zip.ZipEntry'/> objects.</summary>
    /// <seealso cref='OrganicBit.Zip.ZipEntryCollection'/>
    [Serializable()]
    public class ZipEntryCollection : CollectionBase {
        
        /// <summary>Initializes a new instance of <see cref='OrganicBit.Zip.ZipEntryCollection'/>.</summary>
        public ZipEntryCollection() {
        }
        
        /// <summary>Initializes a new instance of <see cref='OrganicBit.Zip.ZipEntryCollection'/> based on another <see cref='OrganicBit.Zip.ZipEntryCollection'/>.</summary>
        /// <param name='value'>A <see cref='OrganicBit.Zip.ZipEntryCollection'/> from which the contents are copied.</param>
        public ZipEntryCollection(ZipEntryCollection value) {
            this.AddRange(value);
        }
        
        /// <summary>Initializes a new instance of <see cref='OrganicBit.Zip.ZipEntryCollection'/> containing any array of <see cref='OrganicBit.Zip.ZipEntry'/> objects.</summary>
        /// <param name='value'>A array of <see cref='OrganicBit.Zip.ZipEntry'/> objects with which to intialize the collection.</param>
        public ZipEntryCollection(ZipEntry[] value) {
            this.AddRange(value);
        }
        
        /// <summary>Represents the entry at the specified index of the <see cref='OrganicBit.Zip.ZipEntry'/>.</summary>
        /// <param name='index'><para>The zero-based index of the entry to locate in the collection.</para></param>
        /// <value>
        ///    <para>The entry at the specified index of the collection.</para>
        /// </value>
        /// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
        public ZipEntry this[int index] {
            get {
                return ((ZipEntry)(List[index]));
            }
            set {
                List[index] = value;
            }
        }
        
        /// <summary>Adds a <see cref='OrganicBit.Zip.ZipEntry'/> with the specified value to the <see cref='OrganicBit.Zip.ZipEntryCollection'/>.</summary>
        /// <param name='value'>The <see cref='OrganicBit.Zip.ZipEntry'/> to add.</param>
        /// <returns>The index at which the new element was inserted.</returns>
        /// <seealso cref='OrganicBit.Zip.ZipEntryCollection.AddRange'/>
        public int Add(ZipEntry value) {
            return List.Add(value);
        }
        
        /// <summary>Copies the elements of an array to the end of the <see cref='OrganicBit.Zip.ZipEntryCollection'/>.</summary>
        /// <param name='value'>An array of type <see cref='OrganicBit.Zip.ZipEntry'/> containing the objects to add to the collection.</param>
        /// <returns>None.</returns>
        /// <seealso cref='OrganicBit.Zip.ZipEntryCollection.Add'/>
        public void AddRange(ZipEntry[] value) {
            for (int i = 0; (i < value.Length); i = (i + 1)) {
                this.Add(value[i]);
            }
        }
        
        /// <summary>Adds the contents of another <see cref='OrganicBit.Zip.ZipEntryCollection'/> to the end of the collection.</summary>
        /// <param name='value'>A <see cref='OrganicBit.Zip.ZipEntryCollection'/> containing the objects to add to the collection.</param>
        /// <returns>None.</returns>
        /// <seealso cref='OrganicBit.Zip.ZipEntryCollection.Add'/>
        public void AddRange(ZipEntryCollection value) {
            for (int i = 0; (i < value.Count); i = (i + 1)) {
                this.Add(value[i]);
            }
        }
        
        /// <summary>Gets a value indicating whether the <see cref='OrganicBit.Zip.ZipEntryCollection'/> contains the specified <see cref='OrganicBit.Zip.ZipEntry'/>.</summary>
        /// <param name='value'>The <see cref='OrganicBit.Zip.ZipEntry'/> to locate.</param>
        /// <returns>
        /// <para><see langword='true'/> if the <see cref='OrganicBit.Zip.ZipEntry'/> is contained in the collection; 
        ///   otherwise, <see langword='false'/>.</para>
        /// </returns>
        /// <seealso cref='OrganicBit.Zip.ZipEntryCollection.IndexOf'/>
        public bool Contains(ZipEntry value) {
            return List.Contains(value);
        }
        
        /// <summary>Copies the <see cref='OrganicBit.Zip.ZipEntryCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the specified index.</summary>
        /// <param name='array'><para>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='OrganicBit.Zip.ZipEntryCollection'/> .</para></param>
        /// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
        /// <returns>None.</returns>
        /// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='OrganicBit.Zip.ZipEntryCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
        /// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
        /// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
        /// <seealso cref='System.Array'/>
        public void CopyTo(ZipEntry[] array, int index) {
            List.CopyTo(array, index);
        }
        
        /// <summary>Returns the index of a <see cref='OrganicBit.Zip.ZipEntry'/> in the <see cref='OrganicBit.Zip.ZipEntryCollection'/>.</summary>
        /// <param name='value'>The <see cref='OrganicBit.Zip.ZipEntry'/> to locate.</param>
        /// <returns>
        /// <para>The index of the <see cref='OrganicBit.Zip.ZipEntry'/> of <paramref name='value'/> in the 
        /// <see cref='OrganicBit.Zip.ZipEntryCollection'/>, if found; otherwise, -1.</para>
        /// </returns>
        /// <seealso cref='OrganicBit.Zip.ZipEntryCollection.Contains'/>
        public int IndexOf(ZipEntry value) {
            return List.IndexOf(value);
        }
        
        /// <summary>Inserts a <see cref='OrganicBit.Zip.ZipEntry'/> into the <see cref='OrganicBit.Zip.ZipEntryCollection'/> at the specified index.</summary>
        /// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
        /// <param name=' value'>The <see cref='OrganicBit.Zip.ZipEntry'/> to insert.</param>
        /// <returns><para>None.</para></returns>
        /// <seealso cref='OrganicBit.Zip.ZipEntryCollection.Add'/>
        public void Insert(int index, ZipEntry value) {
            List.Insert(index, value);
        }
        
        /// <summary>Returns an enumerator that can iterate through the <see cref='OrganicBit.Zip.ZipEntryCollection'/>.</summary>
        /// <returns><para>None.</para></returns>
        /// <seealso cref='System.Collections.IEnumerator'/>
        public new ZipEntryEnumerator GetEnumerator() {
            return new ZipEntryEnumerator(this);
        }
        
        /// <summary>Removes a specific <see cref='OrganicBit.Zip.ZipEntry'/> from the <see cref='OrganicBit.Zip.ZipEntryCollection'/>.</summary>
        /// <param name='value'>The <see cref='OrganicBit.Zip.ZipEntry'/> to remove from the <see cref='OrganicBit.Zip.ZipEntryCollection'/> .</param>
        /// <returns><para>None.</para></returns>
        /// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
        public void Remove(ZipEntry value) {
            List.Remove(value);
        }
        
        /// <summary>Enumerator for <see cref="ZipEntryCollection"/>.</summary>
        public class ZipEntryEnumerator : object, IEnumerator {
            
            private IEnumerator baseEnumerator;
            
            private IEnumerable temp;
            
            /// <summary>Initializes a new instance of the <see cref="ZipEntryEnumerator"/> class.</summary>
            public ZipEntryEnumerator(ZipEntryCollection mappings) {
                this.temp = ((IEnumerable)(mappings));
                this.baseEnumerator = temp.GetEnumerator();
            }
            
            /// <summary>Gets the current entry.</summary>
            public ZipEntry Current {
                get {
                    return ((ZipEntry)(baseEnumerator.Current));
                }
            }
            
            object IEnumerator.Current {
                get {
                    return baseEnumerator.Current;
                }
            }
            

            /// <summary>Advance the enumerator to the next entry in the collection.</summary>
            /// <returns><c>true</c> if there are more entries; <c>false</c> if there are no more entires in the collection.</returns>
            public bool MoveNext() {
                return baseEnumerator.MoveNext();
            }
            
            bool IEnumerator.MoveNext() {
                return baseEnumerator.MoveNext();
            }

            /// <summary>Set the enumerator to just before the start of the collection.  Call <see cref="MoveNext"/> to advance to the first entry in the collection.</summary>
            public void Reset() {
                baseEnumerator.Reset();
            }
            
            void IEnumerator.Reset() {
                baseEnumerator.Reset();
            }
        }
    }
}
