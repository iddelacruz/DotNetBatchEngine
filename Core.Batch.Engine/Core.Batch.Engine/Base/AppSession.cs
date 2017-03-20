﻿using Core.Batch.Engine.Contracts;
using Core.Batch.Engine.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Core.Batch.Engine.Base
{
    /// <summary>
    /// Clase encargada de contener las operaciones a manejar.
    /// </summary>
    /// <remarks>
    /// Esta clase se encargará de manejar el estado de las operaciones
    /// que se harán sobre el Servicio Web de Eurotax.
    /// </remarks>
    public sealed class AppSession : IAppSession
    {
        #region Fields
        static IUnitOfWork _unitOfWork;
        #endregion

        #region Properties
        /// <summary>
        /// Identificador de la sesión.
        /// </summary>
        public Guid SessionID { get; internal set; }

        /// <summary>
        /// Listado de las operaciones que quedan pendientes de ejecutar.
        /// </summary>
        public Queue<IOperation> OperationsRemaining { get; internal set; }

        /// <summary>
        /// Listado con los resultados de las operaciones realizadas.
        /// </summary>
        public List<IOperation> OperationsResult { get; internal set; }

        /// <summary>
        /// Fecha de creación de la sesión.
        /// </summary>
        public DateTime? CreationDate { get; internal set; }

        /// <summary>
        /// Fecha de actualización de la sesión.
        /// </summary>
        public DateTime? UpdateDate { get; internal set; }

        /// <summary>
        /// Estado de la sesión: <see cref="SessionState"/> 
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        public SessionState State { get; set; }

        /// <summary>
        /// Asociación bidireccional entre <see cref="IAppSession"/>
        /// y <see cref="IApplication"/>
        public IApplication App { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        /// Crea una nueva instancia de <see cref="AppSession"/>
        /// </summary>
        public AppSession()
        {
            SessionID = Guid.NewGuid();
            OperationsRemaining = new Queue<IOperation>();
            State = SessionState.Initial;
            CreationDate = DateTime.Now;
            _unitOfWork = new JsonUnitOfWork();
        }
        #endregion

        #region Operations
        /// <summary>
        /// Obtener un objeto <see cref="IAppSession"/> por identififcador.
        /// </summary>
        /// <param name="identifier">Identificador de la sesión.</param>
        public async Task GetAsync(Guid identifier)
        {
            await _unitOfWork.FindAsync(identifier);
        }

        /// <summary>
        /// Agregar operaciones a la sesión.
        /// </summary>
        /// <param name="operation">La operación que se va a agregar.</param>
        /// <returns>Verdadero si se ha añadido correctamente, falso si no.</returns>
        public async Task<bool> RegisterOperationAsync(IOperation operation)
        {
            return await Task.Run(() => 
            {
                if ((operation != null) && (!OperationsRemaining.Contains(operation)))
                {
                    OperationsRemaining.Enqueue(operation);
                    return true;
                }
                else
                {
                    return false;
                }
            });
        }

        /// <summary>
        /// Almacena en memoria los cambios sobre cada una de las operaciones.
        /// </summary>
        /// <param name="operation">Operación a almacenar.</param>
        public async Task StoreAsync(IOperation operation)
        {
            await Task.Run(() => 
            {
                if (operation == null)
                {
                    throw new ArgumentNullException("operation");
                }

                if (OperationsResult == null)
                {
                    OperationsResult = new List<IOperation>();
                    OperationsResult.Add(operation);
                }
                else
                {
                    var index = OperationsResult
                    .IndexOf
                    (OperationsResult
                    .Where(x => x.OperationID.Equals(operation.OperationID))
                    .FirstOrDefault());

                    if (index == -1)
                    {
                        OperationsResult.Add(operation);
                    }
                    else
                    {
                        OperationsResult[index] = operation;
                        UpdateDate = DateTime.Now;
                    }
                }
            });
        }

        /// <summary>
        /// Encargado de persistir la sesión.
        /// </summary>
        public async Task FlushAsync()
        {
            await _unitOfWork.PersistAsync(this);
        }

        /// <summary>
        /// Recupera una sesión persistida que tenga el estado 
        /// <see cref="SessionState.Uncompleted"/>
        /// </summary>
        /// <returns>La sesión recuperada.</returns>
        public static async Task<IAppSession> RecoverAsync()
        {
            return await _unitOfWork.RecoverAsync();
        }

        #region Dispose pattern
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_unitOfWork != null)
                {
                    _unitOfWork.Dispose();
                }
            }
        }
        #endregion
        #endregion
    }
}