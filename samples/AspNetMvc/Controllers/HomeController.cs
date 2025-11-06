// Copyright (c) Andrew Arnott. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc;

[Controller]
public class HomeController : Controller
{
    public IActionResult Index() => this.View();
}
