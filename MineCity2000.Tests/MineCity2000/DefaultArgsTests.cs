using com.mc2k.MineCity2000;

namespace MineCity2000.Tests;

public class DefaultArgsTests
{
  [Fact]
  public void value_with_spaces_is_kept() => Assert.Equal(new[] { "-o", "/a b/c" }, DefaultArgs.parse("-o /a b/c"));

  [Fact]
  public void crlf_and_lf_both_work() => Assert.Equal(new[] { "-f", "-g" }, DefaultArgs.parse("-f\r\n-g\n"));

  [Fact]
  public void comments_and_blank_lines_are_skipped() => Assert.Empty(DefaultArgs.parse("# hi\n\n   \n"));

  [Fact]
  public void option_without_value_is_a_single_token() => Assert.Equal(new[] { "-o" }, DefaultArgs.parse("-o"));

  [Fact]
  public void extra_spaces_around_value_are_trimmed() => Assert.Equal(new[] { "-o", "x" }, DefaultArgs.parse("-o    x  "));

  [Fact]
  public void tab_between_option_and_value_is_not_a_separator() => Assert.Equal(new[] { "-o\t/x" }, DefaultArgs.parse("-o\t/x"));
}
