# Product

## Register

product

## Users

Six distinct operator roles, each with a different primary task and context:

- **Admin**: School director or office manager. Configures the system, manages HR (teachers, invite links), has full access to all modules. Desktop-first, intermittent throughout the day.
- **Teacher**: Classroom teacher. Takes attendance (QR scan or manual), enters grades per the Vietnamese TT22 grading standard, monitors class rosters. Used daily, often quickly between classes.
- **Accountant**: Finance staff. Generates tuition invoices, tracks outstanding debt per student, manages fee schedules. Desktop, focused workflow blocks.
- **Supervisor (Giám thị)**: Disciplinary supervisor. Logs behavioral records, sends messages to parents. Moderate frequency.
- **Student / Parent**: Consumers of academic records. Read-only access to grades, attendance, and announcements. (Brand surface also serves prospective schools evaluating the platform.)

All users operate in Vietnamese. The product is multi-tenant: each school instance is data-isolated via `SchoolId`.

## Product Purpose

Lumina Tutors is a multi-tenant education management system for Vietnamese private schools. It replaces paper-based and fragmented administrative workflows with a single platform covering: class and enrollment management, QR-based and manual attendance (with face recognition roadmap), TT22-standard grade books, tuition invoicing and debt tracking, HR and teacher onboarding, discipline records, and parent communication.

Success looks like: a teacher taking attendance in under 30 seconds; an accountant generating and filtering 200 invoices without a spreadsheet; a school director onboarding a new teacher with a single invite link.

The brand surface (landing / acquisition) presents Lumina Tutors to school administrators who are evaluating whether to adopt it for their institution.

## Brand Personality

Authoritative luxury. The product should feel like a high-end professional suite used by people who take their work seriously. It earns trust through precision and restraint, not decoration. Three words: **Authoritative, Refined, Trustworthy**.

Tone: composed, institutional, never casual. Vietnamese copy should read as formal professional prose, not startup-friendly chat. The brand surface can be warmer (aspirational, inviting), but the product core stays disciplined.

## Anti-references

- Generic SaaS bootstrapped dashboards: white backgrounds, blue primary buttons, flat icons, zero personality. The kind of UI that could be any tool in any industry.
- Playful edtech: bright primary colors, gamification badges, Duolingo-style illustrations. This is for school staff, not students in a learning app.
- Enterprise grey: heavy SharePoint / legacy ERP energy. Dense grey forms, no visual hierarchy, no identity.
- Neon dark fintech: dark backgrounds with cyan/green neon accents. Crypto trading platform aesthetic has no place in a Vietnamese school.

## Design Principles

1. **Institutional precision over friendly casualness.** Every visual choice should reinforce that this is a serious tool used by professionals accountable for real student outcomes. Warmth is earned through reliability, not decoration.

2. **Role-shaped surfaces.** A teacher's attendance view and an accountant's invoice list are completely different jobs. Each surface should be designed for its specific operator, not averaged toward a generic layout.

3. **Data density with clear hierarchy.** School management involves a lot of information. The goal is not to hide it, but to structure it so the most important signal is always obvious at a glance. Hierarchy through scale and weight, not color noise.

4. **Vietnamese-first.** Language, cultural norms, and regulatory context (TT22 grading, Vietnamese fiscal formats) are native, not bolted on. Copy reads as proper formal Vietnamese, not translated English.

5. **Earned elegance.** Luxury in the UI comes from precision: consistent rhythm, exact spacing, deliberate typographic choices, not gradient overlays or ornamental excess.

## Accessibility & Inclusion

WCAG 2.1 AA. Keyboard navigability for all interactive elements. Sufficient color contrast for text and UI components (4.5:1 for normal text, 3:1 for large text and UI). Avoid communicating state through color alone — pair with text or icon. No motion for users with `prefers-reduced-motion`.
