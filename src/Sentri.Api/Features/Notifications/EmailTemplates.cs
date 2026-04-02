using System;
using Sentri.Api.Domain.Events;

namespace Sentri.Api.Features.Notifications;

public static class EmailTemplates
{
    public static string GetWarningThresholdEmailContent(WarningThresholdReachedDomainEvent domainEvent)
    {
        return $@"
<html>
<head>
  <meta charset='utf-8'>
  <meta name='viewport' content='width=device-width, initial-scale=1'>
</head>
<body style='margin:0;padding:24px;background:#f0ece4;font-family:Courier,monospace;'>

  <table width='100%' cellpadding='0' cellspacing='0' border='0' style='max-width:560px;margin:0 auto;'>
  <tr><td>
  <table width='100%' cellpadding='0' cellspacing='0' border='0'
    style='background:#fff9f0;border:1.5px solid #e8ddd0;border-radius:4px;'>

    <!-- HEADER -->
    <tr>
      <td style='background:#1a1209;padding:28px 32px 24px;'>
        <table cellpadding='0' cellspacing='0' border='0'>
          <tr>
            <td style='background:#33291100;border:1px solid #f5a62355;border-radius:2px;padding:4px 10px;'>
              <table cellpadding='0' cellspacing='0' border='0'><tr>
                <td style='vertical-align:middle;padding-right:6px;'>
                    <div style='width:6px;height:6px;background:#f5a623;border-radius:50%;line-height:1;font-size:0;'></div>
                </td>
                <td style='font-size:10px;letter-spacing:0.12em;text-transform:uppercase;color:#f5a623;padding-left:6px;'>Budget Alert</td>
              </tr></table>
            </td>
          </tr>
          <tr><td style='height:14px;'></td></tr>
          <tr>
            <td style='font-family:Georgia,serif;font-weight:600;font-size:26px;color:#fff9f0;line-height:1.2;'>
              Threshold<br>Reached
            </td>
          </tr>
        </table>
      </td>
    </tr>

    <!-- BODY -->
    <tr>
      <td style='padding:28px 32px;'>

        <!-- Provider -->
        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:24px;'>
          <tr>
            <td style='background:#f5f0e8;border-left:3px solid #f5a623;padding:14px 18px;'>
              <div style='font-size:10px;letter-spacing:0.1em;text-transform:uppercase;color:#9a8c7a;margin-bottom:4px;'>Provider</div>
              <div style='font-family:Georgia,serif;font-size:18px;font-weight:600;color:#1a1209;'>{domainEvent.ProviderName}</div>
              <div style='font-size:11px;color:#9a8c7a;margin-top:2px;'>{domainEvent.ProviderId}</div>
            </td>
          </tr>
        </table>

        <!-- Threshold row -->
        <table width='100%' cellpadding='0' cellspacing='0' border='0'
          style='margin-bottom:20px;padding-bottom:20px;border-bottom:1px dashed #e0d5c5;'>
          <tr>
            <td style='font-size:11px;letter-spacing:0.08em;text-transform:uppercase;color:#9a8c7a;vertical-align:middle;'>Warning Threshold</td>
            <td align='right' style='font-family:Georgia,serif;font-size:28px;font-weight:300;color:#d4870a;vertical-align:middle;'>{domainEvent.WarningThreshold:P0}</td>
          </tr>
        </table>

        <!-- Progress label -->
        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:8px;'>
          <tr>
            <td style='font-size:11px;color:#9a8c7a;letter-spacing:0.06em;text-transform:uppercase;'>Budget Utilization</td>
            <td align='right' style='font-size:11px;color:#d4870a;font-weight:500;'>{domainEvent.CurrentSpend / domainEvent.MonthlyBudget:P0} used</td>
          </tr>
        </table>

        <!-- Progress bar -->
        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:24px;background:#e8ddd0;border-radius:3px;'>
          <tr>
            <td style='height:6px;font-size:0;line-height:0;'>
              <table cellpadding='0' cellspacing='0' border='0' style='width:{Math.Min(domainEvent.CurrentSpend / domainEvent.MonthlyBudget, 1) * 100:F0}%;'>
                <tr><td style='height:6px;background:#f5a623;border-radius:3px;font-size:0;line-height:0;'>&nbsp;</td></tr>
              </table>
            </td>
          </tr>
        </table>

        <!-- Stat cards -->
        <table width='100%' cellpadding='0' cellspacing='0' border='0' style='margin-bottom:24px;'>
          <tr>
            <td width='48%' style='background:#f5f0e8;border:1px solid #e8ddd0;padding:14px 16px;vertical-align:top;'>
              <div style='font-size:10px;letter-spacing:0.1em;text-transform:uppercase;color:#9a8c7a;margin-bottom:6px;'>Current Spend</div>
              <div style='font-family:Georgia,serif;font-size:20px;font-weight:600;color:#1a1209;'>{domainEvent.CurrentSpend:C}</div>
            </td>
            <td width='4%'></td>
            <td width='48%' style='background:#f5f0e8;border:1px solid #e8ddd0;padding:14px 16px;vertical-align:top;'>
              <div style='font-size:10px;letter-spacing:0.1em;text-transform:uppercase;color:#9a8c7a;margin-bottom:6px;'>Monthly Budget</div>
              <div style='font-family:Georgia,serif;font-size:20px;font-weight:600;color:#1a1209;'>{domainEvent.MonthlyBudget:C}</div>
            </td>
          </tr>
        </table>

        <!-- CTA -->
        <table width='100%' cellpadding='0' cellspacing='0' border='0'>
          <tr>
            <td style='background:#1a1209;border-radius:3px;padding:16px 20px;'>
              <table width='100%' cellpadding='0' cellspacing='0' border='0'><tr>
                <td style='font-size:12px;color:#c8b99a;line-height:1.5;'>
                  Review your infrastructure and AI costs to prevent budget overruns.
                </td>
                <td width='16'></td>
                <td align='right' style='white-space:nowrap;'>
                  <a href='https://www.youtube.com/watch?v=dQw4w9WgXcQ' style='background:#f5a623;color:#1a1209;font-size:11px;font-weight:500;letter-spacing:0.06em;text-transform:uppercase;padding:9px 16px;text-decoration:none;display:inline-block;'>Review Now →</a>
                </td>
              </tr></table>
            </td>
          </tr>
        </table>

      </td>
    </tr>

    <!-- FOOTER -->
    <tr>
      <td style='padding:16px 32px;border-top:1px solid #e8ddd0;'>
        <table width='100%' cellpadding='0' cellspacing='0' border='0'><tr>
          <td style='font-size:10px;color:#b8a990;letter-spacing:0.06em;text-transform:uppercase;'>Automated Alert</td>
          <td align='right' style='font-size:10px;color:#b8a990;letter-spacing:0.06em;text-transform:uppercase;'>SENTRI</td>
        </tr></table>
      </td>
    </tr>

  </table>
  </td></tr>
  </table>

</body>
</html>";
    }
}
