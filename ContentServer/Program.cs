using System;
using System.IO;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace ContentServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // 제공할 파일 폴더 하나를 정합니다. Unity 프로젝트 전체를 지정하지 않습니다.
            // --FilesRoot 인수가 없으면 ??의 오른쪽인 Content 폴더를 사용합니다.
            string folder = builder.Configuration["FilesRoot"] ?? "Content";
            // 상대 경로라면 서버 프로젝트의 콘텐츠 루트를 기준으로 절대 경로를 구합니다.
            string root = Path.GetFullPath(folder, builder.Environment.ContentRootPath);
            if (!Directory.Exists(root))
            {
                throw new DirectoryNotFoundException("제공할 폴더가 없습니다: " + root);
            }

            // using 선언으로 Main이 끝날 때 파일 제공자가 사용한 자원을 정리합니다.
            using PhysicalFileProvider files = new PhysicalFileProvider(root);
            
            // 수업에서 쓰는 형식만 제공합니다. .cs, .csproj 등은 제공하지 않습니다.
            FileExtensionContentTypeProvider types = new FileExtensionContentTypeProvider();
            types.Mappings.Clear();
            // application은 프로그램이 처리할 데이터, octet-stream은 8비트 단위로 이어진 일반 바이너리 데이터를 뜻합니다.
            types.Mappings[".bundle"] = "application/octet-stream";
            types.Mappings[".hash"] = "text/plain";
            types.Mappings[".json"] = "application/json";
            types.Mappings[".html"] = "text/html";
            types.Mappings[".js"] = "application/javascript";
            types.Mappings[".css"] = "text/css";
            types.Mappings[".wasm"] = "application/wasm";
            types.Mappings[".data"] = "application/octet-stream";
            types.Mappings[".png"] = "image/png";
            types.Mappings[".ico"] = "image/x-icon";
            types.Mappings[".svg"] = "image/svg+xml";

            WebApplication app = builder.Build();
            // Web 빌드의 index.html도 같은 서버로 열 수 있게 합니다.
            app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = files });
            // 기본 문서 경로를 먼저 고른 뒤, 실제 파일과 확장자에 맞는 응답 형식을 제공합니다.
            app.UseStaticFiles(new StaticFileOptions
            {
                FileProvider = files,
                ContentTypeProvider = types,
                // no-cache는 받은 응답을 재사용하기 전에 서버에 확인하라는 뜻입니다. Unity 번들 캐시 삭제는 아닙니다.
                OnPrepareResponse = context =>
                    context.Context.Response.Headers.CacheControl = "no-cache"
            });

            app.MapGet("/health", () => Results.Ok(new { status = "ok" }));

            Console.WriteLine("파일 제공 폴더: " + root);
            // 네트워크 과정에서 준비한 localhost 개발 인증서를 사용합니다.
            app.Run("https://localhost:5443");
        }
    }
}