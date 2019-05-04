﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FytSoa.Common;
using FytSoa.Core.Model.Sys;
using FytSoa.Service.DtoModel;
using FytSoa.Service.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace FytSoa.Api.Controllers
{
    [Produces("application/json")]
    [Route("api/Menu")]
    [Authorize(Roles = "Admin")]
    public class MenuController : Controller
    {
        private readonly ISysMenuService _sysMenuService;
        private readonly ISysAuthorizeService _authorizeService;
        private readonly ICacheService _cacheService;
        public MenuController(ISysMenuService sysMenuService, ISysAuthorizeService authorizeService
            , ICacheService cacheService)
        {
            _sysMenuService = sysMenuService;
            _authorizeService = authorizeService;
            _cacheService = cacheService;
        }

        /// <summary>
        /// 根据菜单，获得当前菜单的所有功能权限
        /// </summary>
        /// <returns></returns>
        [HttpGet("bycode"), Log("Menu：bycode", LogType = LogEnum.RETRIEVE)]
        public JsonResult GetCodeByMenu(string role,string menu="all")
        {
            var res = _authorizeService.GetCodeByMenu(role,menu);
            return Json(new { code = 0, msg = "success", count = 1,res.Result.data });
        }

        /// <summary>
        /// 获得组织结构Tree列表
        /// </summary>
        /// <returns></returns>
        [HttpPost("gettree"), Log("Menu：gettree", LogType = LogEnum.RETRIEVE)]
        public List<SysMenuTree> GetListPage(string roleGuid)
        {
            return _sysMenuService.GetListTreeAsync(roleGuid).Result.data;
        }

        /// <summary>
        /// 查询列表
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("getpages"), Log("Menu：getpages", LogType = LogEnum.RETRIEVE)]
        public async Task<JsonResult> GetPages(PageParm parm)
        {
            var res = await _sysMenuService.GetPagesAsync(parm);
            if (res.data?.Items?.Count > 0)
            {
                foreach (var item in res.data.Items)
                {
                    item.Name = Utils.LevelName(item.Name, item.Layer);
                }
            }
            return Json(new { code = 0, msg = "success", count = res.data?.TotalItems, data = res.data?.Items });
        }

        /// <summary>
        /// 提供权限查询
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet("authmenu")]        
        public ApiResult<List<SysMenuDto>> GetAuthMenu()
        {
            var res = new ApiResult<List<SysMenuDto>>();
            //var auth = await HttpContext.AuthenticateAsync();
            //var menu = auth.Principal.Identities.First(u => u.IsAuthenticated).FindFirst(KeyHelper.ADMINMENU).Value;
            //res.data = JsonConvert.DeserializeObject<List<SysMenuDto>>(menu);
            var menuSaveType = ConfigExtensions.Configuration[KeyHelper.LOGINAUTHORIZE];
            if (menuSaveType == "Redis")
            {
                res.data = RedisCacheService.Default.GetCache<List<SysMenuDto>>(KeyHelper.ADMINMENU);
            }
            else
            {
                res.data = MemoryCacheService.Default.GetCache<List<SysMenuDto>>(KeyHelper.ADMINMENU);
            }
            if (res.data==null)
            {
                res.statusCode = (int)ApiEnum.URLExpireError;
                res.message = "Session已过期，请重新登录";
            }
            return res;
        }

        /// <summary>
        /// 添加菜单
        /// </summary>
        /// <returns></returns>
        [HttpPost("add"), ApiAuthorize(Modules = "Menu", Methods = "Add", LogType = LogEnum.ADD)]
        public async Task<ApiResult<string>> AddMenu(SysMenu parm,List<string> cbks)
        {
            return await _sysMenuService.AddAsync(parm,cbks);
        }

        /// <summary>
        /// 删除
        /// </summary>
        /// <returns></returns>
        [HttpPost("delete"), ApiAuthorize(Modules = "Menu", Methods = "Delete", LogType = LogEnum.DELETE)]
        public async Task<ApiResult<string>> DeleteMenu(string parm)
        {
            var list = Utils.StrToListString(parm);
            return await _sysMenuService.DeleteAsync(m => list.Contains(m.Guid));
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <returns></returns>
        [HttpPost("edit"), ApiAuthorize(Modules = "Menu", Methods = "Update", LogType = LogEnum.UPDATE)]
        public async Task<ApiResult<string>> EditMenu(SysMenu parm, List<string> cbks)
        {
            return await _sysMenuService.ModifyAsync(parm,cbks);
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <returns></returns>
        [HttpPost("authorizaion")]
        [ApiAuthorize(Modules = "Menu", Methods = "Update", LogType = LogEnum.STATUS)]
        public async Task<ApiResult<List<SysMenuDto>>> GetAuthorizaionMenu(string roid)
        {
            return await _sysMenuService.GetMenuByRole(roid);
        }
    }
}