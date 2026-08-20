namespace GloryLikeWebApp.Models.Employer;

public static class CompanyAboutPageDefaults
{
    public const string LayoutJson =
        "[\"media\",\"about\",\"culture\",\"benefits\",\"locations\",\"vacancies\",\"contact\"]";

    public const string CustomHtml = """
<main class="custom-company-page" style="max-width:1180px;margin:0 auto;padding:32px 20px;color:#172033;font-family:Inter,Arial,sans-serif">
  <section data-company-section="media" style="overflow:hidden;border-radius:28px;background-color:#eef2ff;box-shadow:0 20px 55px rgba(37,45,79,.12)">
    <img data-company-field="cover" alt="Company cover" style="display:block;width:100%;height:360px;object-fit:cover">
    <div style="display:flex;align-items:center;gap:20px;padding:24px 30px">
      <img data-company-field="logo" alt="Company logo" style="width:92px;height:92px;border-radius:24px;object-fit:cover;background-color:#ffffff">
      <div>
        <h1 data-company-field="company-name" style="margin:0;font-size:42px;line-height:1.1"></h1>
        <p data-company-field="company-meta" style="margin:10px 0 0;color:#667085"></p>
      </div>
    </div>
  </section>
  <section data-company-section="about" style="padding:54px 12px 20px">
    <small style="color:#5b4df5;font-weight:800;letter-spacing:.12em;text-transform:uppercase">About us</small>
    <h2 style="font-size:30px;margin:10px 0 14px">Our company</h2>
    <p data-company-field="description" style="font-size:18px;line-height:1.75;color:#475467"></p>
  </section>
  <section data-company-section="culture" style="display:grid;grid-template-columns:1fr 1fr;gap:22px;padding:20px 12px">
    <article style="padding:28px;border-radius:22px;background-color:#f7f8fc"><h2>Culture &amp; values</h2><p data-company-field="culture" style="line-height:1.7;color:#667085"></p></article>
    <article style="padding:28px;border-radius:22px;background-color:#f1efff"><h2>Why work with us</h2><p data-company-field="why-work" style="line-height:1.7;color:#667085"></p></article>
  </section>
  <section data-company-section="benefits" style="padding:42px 12px"><h2 style="font-size:30px">Benefits</h2><div data-company-field="benefits" style="display:flex;flex-wrap:wrap;gap:12px"></div></section>
  <section data-company-section="locations" style="padding:42px 12px"><h2 style="font-size:30px">Our locations</h2><div data-company-field="locations" style="display:grid;grid-template-columns:repeat(2,1fr);gap:16px"></div></section>
  <section data-company-section="vacancies" style="padding:42px 12px;border-radius:26px;background-color:#181c2c;color:#ffffff"><h2 style="font-size:30px;margin-top:0">Join our team</h2><p>See current opportunities and find your next role.</p><a data-company-field="vacancies-link" style="display:inline-block;margin-top:12px;padding:13px 20px;border-radius:12px;background-color:#6c5ce7;color:#ffffff;text-decoration:none;font-weight:800">View vacancies</a></section>
  <footer data-company-section="contact" style="padding:42px 12px;text-align:center"><a data-company-field="website-link" style="color:#5b4df5;font-weight:800">Visit company website</a></footer>
</main>
""";
}
