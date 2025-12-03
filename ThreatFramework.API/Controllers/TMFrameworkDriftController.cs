using Microsoft.AspNetCore.Mvc;
using ThreatFramework.Drift.Contract.Model;
using ThreatModeler.TF.Core.Global;
using ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal;
using GlobalDrift1 = ThreatModeler.TF.Drift.Contract.Model.UpdatedFinal.GlobalDrift1;

namespace ThreatModeler.TF.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TMFrameworkDriftController11 : ControllerBase
    {
        /// <summary>
        /// Returns framework drift data.
        /// GET: api/TMFrameworkDrift
        /// </summary>
        [HttpGet]
        public ActionResult<TMFrameworkDrift1> GetFrameworkDrift()
        {
            // In real scenario, populate from service/db.
            // Here is a sample object to show structure.
            return Ok(new TMFrameworkDrift1
            {
                ModifiedLibraries = new List<LibraryDrift1>(),
                AddedLibraries = new List<AddedLibrary1>(),
                DeletedLibraries = new List<DeletedLibrary1>(),
                Global = new GlobalDrift1
                {
                    PropertyOptions = new EntityDiff<PropertyOption>(),
                    PropertyTypes = new EntityDiff<PropertyType>(),
                    ComponentTypes = new EntityDiff<ComponentType>()
                }
            });
        }
    }
}