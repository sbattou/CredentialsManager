﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Text;

namespace Simple.CredentialManager
{
    /// <summary>
    ///     Class Credential, wrapper for native CREDENTIAL structure.
    ///     See CREDENTIAL structure
    ///     <see href="http://msdn.microsoft.com/en-us/library/windows/desktop/aa374788(v=vs.85).aspx">documentation.</see>
    ///     See Credential Manager
    ///     <see href="http://windows.microsoft.com/en-us/windows7/what-is-credential-manager">documentation.</see>
    /// </summary>
    public class Credential : IDisposable
    {
        /// <summary>
        ///     The lock object
        /// </summary>
        private static readonly object LockObject = new object();

        /// <summary>
        ///     The unmanaged code permission
        /// </summary>
        private static readonly SecurityPermission UnmanagedCodePermission;

        /// <summary>
        ///     The disposed flag
        /// </summary>
        private bool disposed;

        /// <summary>
        ///     The string that contains the name of the credential
        /// </summary>
        private string target;

        /// <summary>
        ///     The credential type
        /// </summary>
        private CredentialType type;

        /// <summary>
        ///     Initializes UnmanagedCodePermission for the <see cref="Credential" /> class.
        /// </summary>
        static Credential()
        {
            lock (LockObject)
            {
                UnmanagedCodePermission = new SecurityPermission(SecurityPermissionFlag.UnmanagedCode);
            }
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="Credential" /> class.
        /// </summary>
        public Credential()
            : this(null)
        {}

        /// <summary>
        ///     Initializes a new instance of the <see cref="Credential" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        public Credential(string username)
            : this(username, null)
        {}

        /// <summary>
        ///     Initializes a new instance of the <see cref="Credential" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        public Credential(string username, string password)
            : this(username, password, null)
        {}

        /// <summary>
        ///     Initializes a new instance of the <see cref="Credential" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="target">The string that contains the name of the credential.</param>
        public Credential(string username, string password, string target)
            : this(username, password, target, CredentialType.Generic)
        {}

        /// <summary>
        ///     Initializes a new instance of the <see cref="Credential" /> class.
        /// </summary>
        /// <param name="username">The username.</param>
        /// <param name="password">The password.</param>
        /// <param name="target">The string that contains the name of the credential.</param>
        /// <param name="type">The credential type.</param>
        public Credential(string username, string password, string target, CredentialType type)
        {
            Target = target;
            Type = type;
        }

        /// <summary>
        ///     Gets or sets the target.
        /// </summary>
        /// <value>
        ///     The name of the credential. The TargetName and Type members uniquely identify the credential. This member cannot
        ///     be changed after the credential is created. Instead, the credential with the old name should be deleted and the
        ///     credential with the new name created.
        /// </value>
        public string Target
        {
            get
            {
                CheckNotDisposed();
                return target;
            }
            set
            {
                CheckNotDisposed();
                target = value;
            }
        }
     
        /// <summary>
        ///     Gets or sets the type.
        /// </summary>
        /// <value>The type of the credential. This member cannot be changed after the credential is created.</value>
        public CredentialType Type
        {
            get
            {
                CheckNotDisposed();
                return type;
            }
            set
            {
                CheckNotDisposed();
                type = value;
            }
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);

            // Prevent GC Collection since we have already disposed of this object
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Finalizes an instance of the <see cref="Credential" /> class.
        /// </summary>
        ~Credential()
        {
            Dispose(false);
        }

        /// <summary>
        ///     Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing">
        ///     <c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only
        ///     unmanaged resources.
        /// </param>
        private void Dispose(bool disposing)
        {
            disposed = true;
        }

        /// <summary>
        ///     Ensures this instance is not disposed.
        /// </summary>
        /// <exception cref="System.ObjectDisposedException">Credential object is already disposed.</exception>
        private void CheckNotDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException("Credential object is already disposed.");
            }
        }

        /// <summary>
        ///     Deletes this instance.
        /// </summary>
        /// <returns><c>true</c> if credential was deleted properly, <c>false</c> otherwise.</returns>
        /// <exception cref="System.InvalidOperationException">Target must be specified to delete a credential.</exception>
        public bool Delete()
        {
            CheckNotDisposed();
            UnmanagedCodePermission.Demand();

            if (string.IsNullOrEmpty(Target))
                throw new InvalidOperationException("Target must be specified to delete a credential.");

            StringBuilder targetToDelete = string.IsNullOrEmpty(Target)
                ? new StringBuilder()
                : new StringBuilder(Target);

            return NativeMethods.CredDelete(targetToDelete, Type, 0);
        }

        /// <summary>
        ///     Loads this instance.
        /// </summary>
        /// <returns><c>true</c> if credential is load properly, <c>false</c> otherwise.</returns>
        public bool Load()
        {
            CheckNotDisposed();
            UnmanagedCodePermission.Demand();

            IntPtr credPointer;

            var result = NativeMethods.CredRead(Target, Type, 0, out credPointer);
            if (!result)
                return false;

            using (var credentialHandle = new NativeMethods.CriticalCredentialHandle(credPointer))
            {
                LoadInternal(credentialHandle.GetCredential());
            }

            return true;
        }
        
        /// <summary>
        ///     Loads all credentials
        /// </summary>
        public static IEnumerable<Credential> LoadAll()
        {
            UnmanagedCodePermission.Demand();

            return NativeMethods.CredEnumerate()
                .Select(c => new Credential(c.UserName, null, c.TargetName))
                .Where(c=>c.Load());
        }

        /// <summary>
        ///     Loads the internal.
        /// </summary>
        /// <param name="credential">The credential.</param>
        internal void LoadInternal(NativeMethods.CREDENTIAL credential)
        {
            Target = credential.TargetName;
        }
    }
}