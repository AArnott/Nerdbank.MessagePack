// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;
using PolyType;

namespace AspNetMvc.Controllers;

#region Controller
[Route("api/v1/[controller]")]
[Produces("application/x-msgpack")]
[ApiController]
public partial class PersonController : ControllerBase
{
    public ActionResult<IEnumerable<Person>> Get()
    {
        return this.Ok(new Person[]
        {
            new(1, "Person 1"),
            new(2, "Person 2"),
        });
    }

    // GET: api/v1/person/{slug}
    [HttpGet("{id}")]
    public ActionResult<Person> Get(int id)
    {
        return this.Ok(new Person(id, $"Person {id}"));
    }

    [GenerateShape]
    public partial record Person(int Id, string Name);

    // Add an attribute for each top-level type that must be serializable
    // That does not have have its own [GenerateShape] attribute on it.
    // Here, we add `int` because it's taken as a parameter type for an action.
    // Although strictly speaking `int` is already covered implicitly because it
    // also appears as a property on Person.
    [GenerateShapeFor<Person[]>]
    [GenerateShapeFor<int>]
    partial class Witness;
}
#endregion
