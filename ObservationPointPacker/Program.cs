using ObservationPointPacker.Commands;

// コマンドライン引数を解析
if (args.Length < 1)
{
    ShowUsage();
    return 1;
}

switch (args[0])
{
    case "pack":
        return await PackCommand.RunAsync(args[1..]);

    case "diff":
        return await DiffCommand.RunAsync(args[1..]);

    default:
        Console.Error.WriteLine($"不明なコマンドです: {args[0]}");
        ShowUsage();
        return 1;
}

static void ShowUsage()
{
    Console.WriteLine("使用方法: ObservationPointPacker <command> [arguments]");
    Console.WriteLine();
    Console.WriteLine("コマンド:");
    Console.WriteLine("  pack <dataVersion> <outputDir>");
    Console.WriteLine("      観測点データをリリース用の各形式に変換して出力します");
    Console.WriteLine("  diff <beforeJson> <afterJson> <outputPath> [beforeRef] [afterRef]");
    Console.WriteLine("      観測点データの差分を Markdown で出力します");
}
