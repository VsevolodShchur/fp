﻿using System;
using System.Collections.Generic;
using Autofac;
using DocoptNet;
using ResultOf;
using TagsCloudContainer.TagCloudVisualization;
using TagsCloudContainer.WordProcessing;

namespace TagsCloudContainer
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            Result.Of(() => DefaultVisualizationSettings.Create().GetValueOrThrow())
                .Then(CreateUsage)
                .Then(usage => ParseArguments(usage, args))
                .Then(AutofacConfig.ConfigureContainer)
                .Then(RunProgramWithContainer)
                .Then(Console.WriteLine)
                .OnFail(Console.WriteLine);
        }

        private static Result<IDictionary<string, ValueObject>> ParseArguments(string usage, string[] args)
        {
            var argumentsResult = Result.Of(() => new Docopt().Apply(usage, args, exit: true));
            if (!argumentsResult.IsSuccess)
                return Result.Fail<IDictionary<string, ValueObject>>($"DocoptNet error: {argumentsResult.Error}");
            var arguments = argumentsResult.GetValueOrThrow();
            return arguments["--help"].IsTrue 
                ? Result.Fail<IDictionary<string, ValueObject>>(usage) 
                : Result.Ok(arguments);
        }
        
        private static Result<string> RunProgramWithContainer(IContainer container)
        {
            var wordProvider = container.Resolve<IWordProvider>();
            var wordNormalizer = container.Resolve<IWordNormalizer>();
            var wordFilter = container.Resolve<IWordFilter>();
            var wordCalculator = container.Resolve<IWordStatisticsCalculator>();
            var layouter = container.Resolve<ILayouter>();
            var visualizer = container.Resolve<IVisualizer>();
            var imgSaver = container.Resolve<IImageSaver>();

            return Result.Of(() => wordProvider.GetWords())
                .Then(words => wordNormalizer.NormalizeWords(words.GetValueOrThrow()))
                .Then(words => wordFilter.Filter(words))
                .Then(words => wordCalculator.CalculateStatistics(words))
                .Then(wordData => layouter.PlaceWords(wordData))
                .Then(tagCloudItems => visualizer.Visualize(tagCloudItems))
                .Then(bitmap => imgSaver.Save(bitmap))
                .Then(savedImgPath => $"Tag cloud saved at {savedImgPath}");
        }

        private static string CreateUsage(DefaultVisualizationSettings defaultSettings)
        {
       return $@"Tag Cloud.
        Usage:
          tag_cloud.exe [<inputFile>] [--format=<format>] [--font=<font>] [--fontSize=<fontSize>] [--bgcolor=<bgcolor>]
 [--textcolor=<textcolor>] [--size=<width>x<height>]
          tag_cloud.exe (-d | --debug) [<inputFile>] [--format=<format>] [--font=<font>] [--fontSize=<fontSize>] 
 [--bgcolor=<bgcolor>] [--textcolor=<textcolor>] [--size=<width>x<height>] [--dbgrectcolor=<dbgrectcolor>]
 [--dbgmarkcolor=<dbgmarkcolor>]
          tag_cloud.exe (-h | --help)

        Options:
          -h --help                       Show this screen.
          --format=<format>               Set image format [default: {defaultSettings.Format}]
          --font=<font>                   Set font [default: {defaultSettings.FontName}]
          --fontSize=<fontSize>           Set size of font [default: {defaultSettings.FontSize}]
          --bgcolor=<bgcolor>             Set background color [default: {defaultSettings.BackgroundColorName}]
          --textcolor=<textcolor>         Set text color [default: {defaultSettings.TextColorName}]
          --size=<width>x<height>         Set size of image [default: {defaultSettings.SizeOfImage}]
          -d --debug                      Enable debug mode
          --dbgrectcolor=<dbgrectcolor>   Set word rectangle color [default: {defaultSettings.DebugItemBoundsColorName}]
          --dbgmarkcolor=<dbgmarkcolor>   Set marking color [default: {defaultSettings.DebugMarkingColorName}]
       ";
        }
    }
}