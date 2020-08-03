using System;
using System.Collections.Generic;
using System.Text;

namespace Ipfs.Hypermedia.Extensions
{
    /// <summary>
    ///   Extensions for List which contains Ipfs.Hypermedia entities.
    ///   Adds method AddWithParent.
    /// </summary>
    /// <remarks>
    ///   Used to automaticly add parent to child entity while adding child to list of entities of parent.
    /// </remarks>
    public static class ListExtensions
    {
        /// <summary>
        ///   List extension for <see cref="File.Blocks">File entities</see>.
        /// </summary>
        /// <param name="blocks">
        ///   List of <see cref="Block">blocks</see> which is extended.
        /// </param>
        /// <param name="child">
        ///   Child <see cref="Block">block</see> which is going to be added to List of blocks.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="File">file</see> for block.
        /// </param>
        public static void AddWithParent(this List<Block> blocks, Block child, File parent)
        {
            child.Parent = parent;
            blocks.Add(child);
        }
        /// <summary>
        ///   List extension for <see cref="Directory.Entities">Directory entities</see>.
        /// </summary>
        /// <param name="entities">
        ///   List of <see cref="ISystemEntity">system entities</see> which is extended.
        /// </param>
        /// <param name="child">
        ///   Child <see cref="File">file</see> which is going to be added to List of system entities.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="Directory">directory</see> for file.
        /// </param>
        public static void AddWithParent(this List<ISystemEntity> entities, File child, Directory parent)
        {
            child.Parent = parent;
            entities.Add(child);
        }
        /// <summary>
        ///   List extension for <see cref="Directory.Entities">Directory entities</see>.
        /// </summary>
        /// <param name="entities">
        ///   List of <see cref="ISystemEntity">system entities</see> which is extended.
        /// </param>
        /// <param name="child">
        ///   Child <see cref="Directory">directory</see> which is going to be added to List of system entities.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="Directory">directory</see> for directory.
        /// </param>
        public static void AddWithParent(this List<ISystemEntity> entities, Directory child, Directory parent)
        {
            child.Parent = parent;
            entities.Add(child);
        }
        /// <summary>
        ///   List extension for <see cref="Hypermedia.Entities">Hypermedia entities</see>.
        /// </summary>
        /// <param name="entities">
        ///   List of <see cref="IEntity">entities</see> which is extended.
        /// </param>
        /// <param name="child">
        ///   Child <see cref="IEntity">directory</see> which is going to be added to List of entities.
        /// </param>
        /// <param name="parent">
        ///   Parent <see cref="Hypermedia">hypermedia</see> for entity.
        /// </param>
        public static void AddWithParent(this List<IEntity> entities, IEntity child, Hypermedia parent)
        {
            child.Parent = parent;
            entities.Add(child);
        }
    }
}
