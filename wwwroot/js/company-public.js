(() => {
    const allowedTags = new Set([
        "A", "ARTICLE", "ASIDE", "BLOCKQUOTE", "BR", "DIV", "EM",
        "FIGCAPTION", "FIGURE", "FOOTER", "H1", "H2", "H3", "H4",
        "HEADER", "HR", "IMG", "LI", "MAIN", "NAV", "OL", "P",
        "SECTION", "SMALL", "SPAN", "STRONG", "UL"
    ]);
    const allowedAttributes = new Set([
        "alt", "aria-hidden", "aria-label", "class", "data-company-field",
        "data-company-section", "role", "style", "title"
    ]);
    const allowedCssProperties = new Set([
        "align-items", "background-color", "border", "border-color",
        "border-radius", "border-style", "border-width", "box-shadow",
        "color", "column-gap", "display", "flex", "flex-basis",
        "flex-direction", "flex-grow", "flex-shrink", "flex-wrap",
        "font-family", "font-size", "font-style", "font-weight", "gap",
        "grid-template-columns", "height", "justify-content",
        "letter-spacing", "line-height", "margin", "margin-bottom",
        "margin-left", "margin-right", "margin-top", "max-height",
        "max-width", "min-height", "min-width", "object-fit", "opacity",
        "overflow", "padding", "padding-bottom", "padding-left",
        "padding-right", "padding-top", "row-gap", "text-align",
        "text-decoration", "text-transform", "white-space", "width"
    ]);

    const cleanPreviewHtml = html => {
        const documentValue = new DOMParser().parseFromString(
            `<div id="company-custom-root">${String(html || "")}</div>`,
            "text/html");
        const root = documentValue.getElementById("company-custom-root");
        if (!root) return "";

        Array.from(root.querySelectorAll("*")).forEach(element => {
            if (!allowedTags.has(element.tagName)) {
                element.remove();
                return;
            }

            Array.from(element.attributes).forEach(attribute => {
                if (!allowedAttributes.has(attribute.name.toLowerCase()))
                    element.removeAttribute(attribute.name);
            });

            const style = element.getAttribute("style") || "";
            const safeStyle = style.split(";").map(item => item.trim()).filter(Boolean)
                .filter(item => {
                    const separator = item.indexOf(":");
                    if (separator <= 0) return false;
                    const property = item.slice(0, separator).trim().toLowerCase();
                    const value = item.slice(separator + 1);
                    return allowedCssProperties.has(property)
                        && !/url\s*\(|expression\s*\(|javascript\s*:|@import/i.test(value);
                }).join(";");
            if (safeStyle) element.setAttribute("style", safeStyle);
            else element.removeAttribute("style");
        });

        return root.innerHTML;
    };

    const initials = name => String(name || "Company")
        .trim().split(/\s+/).slice(0, 2)
        .map(part => part.charAt(0).toUpperCase()).join("") || "CO";

    const safeHttpUrl = value => {
        try {
            const parsed = new URL(String(value || ""), window.location.origin);
            return ["http:", "https:"].includes(parsed.protocol) ? parsed.href : "";
        } catch { return ""; }
    };

    const create = (tag, className, text) => {
        const element = document.createElement(tag);
        if (className) element.className = className;
        if (text) element.textContent = text;
        return element;
    };

    const profileMeta = profile => [
        profile.companyType,
        profile.activityScope,
        profile.employeeCount ? `${profile.employeeCount} employees` : ""
    ].filter(Boolean).join(" · ");

    const renderMedia = profile => {
        const section = create("section", "company-public-section company-public-media");
        if (profile.coverImageDataUrl) {
            const cover = create("img", "company-public-cover");
            cover.src = profile.coverImageDataUrl;
            cover.alt = `${profile.companyName || "Company"} cover`;
            section.append(cover);
        }
        const identity = create("div", "company-public-identity");
        let logo;
        if (profile.logoDataUrl) {
            logo = create("img", "company-public-logo");
            logo.src = profile.logoDataUrl;
            logo.alt = `${profile.companyName || "Company"} logo`;
        } else {
            logo = create("div", "company-public-logo company-public-logo-fallback", initials(profile.companyName));
        }
        const copy = create("div");
        copy.append(create("h1", "", profile.companyName || "Company"));
        copy.append(create("p", "", profileMeta(profile) || "Company career page"));
        identity.append(logo, copy);
        section.append(identity);
        return section;
    };

    const renderAbout = profile => {
        const section = create("section", "company-public-section company-public-card");
        section.append(create("p", "company-public-eyebrow", "About us"));
        section.append(create("h2", "", "Our company"));
        section.append(create("p", "company-public-copy", profile.companyDescription || "Company description will appear here."));
        return section;
    };

    const renderCulture = profile => {
        const section = create("section", "company-public-section company-public-two-column");
        [["Culture & values", profile.companyCulture], ["Why work with us", profile.whyWorkWithUs]]
            .forEach(([title, value]) => {
                const card = create("article", "company-public-feature");
                card.append(create("h2", "", title));
                card.append(create("p", "company-public-copy", value || "More information coming soon."));
                section.append(card);
            });
        return section;
    };

    const renderBenefits = profile => {
        const section = create("section", "company-public-section company-public-card");
        section.append(create("p", "company-public-eyebrow", "What we offer"));
        section.append(create("h2", "", "Benefits"));
        const list = create("div", "company-public-benefits");
        const benefits = Array.isArray(profile.benefits) ? profile.benefits : [];
        (benefits.length ? benefits : ["Benefits will be announced soon."])
            .forEach(value => list.append(create("span", "company-public-benefit", value)));
        section.append(list);
        return section;
    };

    const locationText = location => [location.address, location.city, location.country]
        .filter(Boolean).join(", ");

    const renderLocations = profile => {
        const section = create("section", "company-public-section company-public-card");
        section.append(create("p", "company-public-eyebrow", "Where we work"));
        section.append(create("h2", "", "Our locations"));
        const list = create("div", "company-public-locations");
        const locations = Array.isArray(profile.locations) ? profile.locations : [];
        locations.forEach((location, index) => {
            const card = create("article", "company-public-location");
            card.append(create("strong", "", location.name || `Location ${index + 1}`));
            card.append(create("span", "", locationText(location) || "Location details coming soon."));
            list.append(card);
        });
        if (!locations.length) list.append(create("p", "company-public-copy", "Locations will be added soon."));
        section.append(list);
        return section;
    };

    const renderVacancies = config => {
        const section = create("section", "company-public-section company-public-cta");
        const copy = create("div");
        copy.append(create("h2", "", "Join our team"));
        copy.append(create("p", "", `${Number(config.vacancyCount || 0)} open role${Number(config.vacancyCount) === 1 ? "" : "s"} available.`));
        const link = create("a", "company-public-button", "View vacancies");
        link.href = config.vacanciesUrl || "#";
        copy.dataset.companyContent = "";
        section.append(copy, link);
        return section;
    };

    const renderContact = profile => {
        const section = create("footer", "company-public-section company-public-contact");
        const website = safeHttpUrl(profile.website);
        if (website) {
            const link = create("a", "", "Visit website");
            link.href = website; link.target = "_blank"; link.rel = "noopener noreferrer";
            section.append(link);
        }
        const socials = [profile.linkedInUrl, profile.instagramUrl, profile.facebookUrl, profile.youtubeUrl, profile.telegramUrl, profile.tiktokUrl]
            .map(safeHttpUrl).filter(Boolean);
        socials.forEach((url, index) => {
            const link = create("a", "", `Social ${index + 1}`);
            link.href = url; link.target = "_blank"; link.rel = "noopener noreferrer";
            section.append(link);
        });
        return section;
    };

    const standardSectionFactories = {
        media: (profile) => renderMedia(profile),
        about: (profile) => renderAbout(profile),
        culture: (profile) => renderCulture(profile),
        benefits: (profile) => renderBenefits(profile),
        locations: (profile) => renderLocations(profile),
        vacancies: (_profile, config) => renderVacancies(config),
        contact: (profile) => renderContact(profile)
    };

    const layout = raw => {
        const fallback = Object.keys(standardSectionFactories);
        try {
            const parsed = JSON.parse(raw || "[]");
            if (!Array.isArray(parsed)) return fallback;
            const valid = parsed.filter((item, index) => standardSectionFactories[item] && parsed.indexOf(item) === index);
            fallback.forEach(item => { if (!valid.includes(item)) valid.push(item); });
            return valid;
        } catch { return fallback; }
    };

    const fillCustom = (container, profile, config) => {
        const text = (field, value) => container.querySelectorAll(`[data-company-field="${field}"]`)
            .forEach(element => { element.textContent = value || ""; });
        text("company-name", profile.companyName || "Company");
        text("company-meta", profileMeta(profile));
        text("description", profile.companyDescription || "Company description will appear here.");
        text("culture", profile.companyCulture || "Culture information coming soon.");
        text("why-work", profile.whyWorkWithUs || "More information coming soon.");

        container.querySelectorAll('[data-company-field="cover"]').forEach(element => {
            if (element instanceof HTMLImageElement && profile.coverImageDataUrl) element.src = profile.coverImageDataUrl;
            else if (!profile.coverImageDataUrl) element.remove();
        });
        container.querySelectorAll('[data-company-field="logo"]').forEach(element => {
            if (element instanceof HTMLImageElement && profile.logoDataUrl) element.src = profile.logoDataUrl;
            else if (element instanceof HTMLImageElement) {
                element.replaceWith(create("div", "company-public-logo company-public-logo-fallback", initials(profile.companyName)));
            }
        });
        container.querySelectorAll('[data-company-field="benefits"]').forEach(list => {
            list.replaceChildren();
            (profile.benefits || []).forEach(value => list.append(create("span", "company-public-benefit", value)));
        });
        container.querySelectorAll('[data-company-field="locations"]').forEach(list => {
            list.replaceChildren();
            (profile.locations || []).forEach((location, index) => {
                const card = create("article", "company-public-location");
                card.append(create("strong", "", location.name || `Location ${index + 1}`));
                card.append(create("span", "", locationText(location)));
                list.append(card);
            });
        });
        container.querySelectorAll('[data-company-field="vacancies-link"]').forEach(link => {
            link.setAttribute("href", config.vacanciesUrl || "#");
        });
        container.querySelectorAll('[data-company-field="website-link"]').forEach(link => {
            const url = safeHttpUrl(profile.website);
            if (!url) link.remove();
            else { link.setAttribute("href", url); link.setAttribute("target", "_blank"); link.setAttribute("rel", "noopener noreferrer"); }
        });
    };

    const render = (container, profile, config = {}) => {
        if (!container) return;
        container.replaceChildren();
        const safeProfile = profile || {};

        if (safeProfile.useCustomAboutPageHtml && safeProfile.aboutPageCustomHtml) {
            const custom = create("div", "company-public-custom");
            custom.innerHTML = cleanPreviewHtml(safeProfile.aboutPageCustomHtml);
            fillCustom(custom, safeProfile, config);
            container.append(custom);
            return;
        }

        const page = create("div", "company-public-page");
        layout(safeProfile.aboutPageLayoutJson).forEach(key => {
            page.append(standardSectionFactories[key](safeProfile, config));
        });
        container.append(page);
    };

    window.CompanyPageRenderer = { render, cleanPreviewHtml };

    document.querySelectorAll("[data-company-public-root]").forEach(container => {
        const sourceId = container.dataset.companySource;
        const source = document.getElementById(sourceId);
        if (!source) return;
        try {
            const payload = JSON.parse(source.textContent || "{}");
            render(container, payload.profile, payload.config);
        } catch {
            container.textContent = "Company page could not be rendered.";
        }
    });
})();
