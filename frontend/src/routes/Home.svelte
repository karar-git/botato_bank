<script>
    import { onMount } from "svelte";
    import { fly, fade, scale } from "svelte/transition";
    import { cubicOut } from "svelte/easing";
    import { tweened } from "svelte/motion";
    import {
        ArrowRight,
        ChevronRight,
        Check,
        Zap,
        Globe,
        ShieldCheck,
    } from "lucide-svelte";
    import Navbar from "../Components/Navbar.svelte";

    import creditCardImg from "../assets/CreditCard.png";

    export let navigate;

    let mounted = false;

    onMount(() => {
        mounted = true;
    });

    const usersCount = tweened(0, { duration: 3000, easing: cubicOut });
    const transactionSpeed = tweened(0, { duration: 2500, easing: cubicOut });

    let statsSection;
    let statsVisible = false;

    $: if (statsSection && !statsVisible) {
        const observer = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting && !statsVisible) {
                        statsVisible = true;
                        usersCount.set(50000);
                        transactionSpeed.set(99.9);
                    }
                });
            },
            { threshold: 0.3 },
        );
        observer.observe(statsSection);
    }

    const sectionDelay = 150;
</script>

<svelte:head>
    <title>Atomic Bank - The Future of Banking</title>
</svelte:head>

<Navbar {navigate} />

<main class="home-container">
    <!-- 1. HERO SECTION -->
    <section class="hero" id="hero">
        {#if mounted}
            <div class="hero-content grid-2">
                <div
                    class="hero-text"
                    in:fly={{
                        y: 30,
                        duration: 800,
                        delay: 100,
                        easing: cubicOut,
                    }}
                >
                    <div class="test-gradient-text"></div>
                    <h1>
                        Atomic <span class="text-gradient">Precision.</span><br
                        />
                        Global <span class="text-gradient">Scale.</span>
                    </h1>
                    <p>
                        Experience the future of finance with instant coherence,
                        atomic security, and zero-friction transactions across
                        the globe.
                    </p>

                    <div class="cta-group">
                        <button
                            class="cta-primary"
                            on:click={() => navigate("login")}
                        >
                            <span>Get Started</span>
                            <div class="icon-wrapper">
                                <ArrowRight size={20} />
                            </div>
                        </button>
                    </div>
                </div>

                <div
                    class="hero-image"
                    in:fly={{
                        y: 50,
                        duration: 1000,
                        delay: 300,
                        easing: cubicOut,
                    }}
                >
                    <div
                        in:scale={{
                            duration: 1000,
                            delay: 300,
                            start: 0.8,
                            easing: cubicOut,
                        }}
                    >
                        <img
                            src={creditCardImg}
                            alt="Atomic Bank Credit Card"
                            class="floating-card"
                        />
                    </div>
                </div>
            </div>
        {/if}
    </section>

    <!-- 2. ABOUT US SECTION -->
    <section class="section-solid about-us" id="about">
        {#if mounted}
            <div
                class="container about-container"
                in:fly={{
                    y: 30,
                    duration: 800,
                    delay: sectionDelay,
                    easing: cubicOut,
                }}
            >
                <div class="section-header">
                    <h2>About Us</h2>
                    <div class="divider"></div>
                </div>
                <div class="glass-panel about-panel">
                    <p>
                        Atomic Bank was founded on a simple principle: Financial
                        transactions should be as fundamental and reliable as
                        physics itself. We are rebuilding the banking stack from
                        the ground up, replacing legacy friction with atomic
                        precision.
                    </p>
                </div>
            </div>
        {/if}
    </section>

    <!-- 3. FEATURES -->
    <section class="section-light features">
        {#if mounted}
            <div
                class="container"
                in:fly={{
                    y: 30,
                    duration: 800,
                    delay: sectionDelay * 2,
                    easing: cubicOut,
                }}
            >
                <div class="section-header">
                    <h2>Revolutionizing Banking</h2>
                    <div class="divider"></div>
                </div>
                <div class="grid-3">
                    <div class="gradient-border-container">
                        <div class="gradient-border-content feature-card">
                            <div class="icon-box">
                                <Zap size={32} color="#3498DB" />
                            </div>
                            <h3>Instant Sync</h3>
                            <p>Real-time coherence across all your devices.</p>
                        </div>
                    </div>
                    <div class="gradient-border-container">
                        <div class="gradient-border-content feature-card">
                            <div class="icon-box">
                                <ShieldCheck size={32} color="#F8C957" />
                            </div>
                            <h3>Zero Friction</h3>
                            <p>Seamless flows. Bank-grade security.</p>
                        </div>
                    </div>
                    <div class="gradient-border-container">
                        <div class="gradient-border-content feature-card">
                            <div class="icon-box">
                                <Globe size={32} color="#34495E" />
                            </div>
                            <h3>Global Access</h3>
                            <p>Your capital, available anywhere on Earth.</p>
                        </div>
                    </div>
                </div>
            </div>
        {/if}
    </section>

    <!-- 4. HOW IT WORKS -->
    <section class="section-light how-it-works">
        {#if mounted}
            <div
                class="container"
                in:fly={{
                    y: 30,
                    duration: 800,
                    delay: sectionDelay * 3,
                    easing: cubicOut,
                }}
            >
                <div class="section-header">
                    <h2>How It Works</h2>
                    <div class="divider"></div>
                </div>
                <div class="steps-container">
                    <div class="step-card glass-panel">
                        <div class="step-num">01</div>
                        <h4>Connect</h4>
                        <p>Link your verified identity securely in seconds.</p>
                    </div>
                    <div class="step-arrow">
                        <ChevronRight size={24} color="#BDC3C7" />
                    </div>
                    <div class="step-card glass-panel">
                        <div class="step-num">02</div>
                        <h4>Deposit</h4>
                        <p>Transfer funds with absolute atomic precision.</p>
                    </div>
                    <div class="step-arrow">
                        <ChevronRight size={24} color="#BDC3C7" />
                    </div>
                    <div class="step-card glass-panel">
                        <div class="step-num">03</div>
                        <h4>Transact</h4>
                        <p>Experience instant global payments anywhere.</p>
                    </div>
                </div>
            </div>
        {/if}
    </section>

    <!-- 5. STATS -->
    <section class="section-solid stats" bind:this={statsSection}>
        {#if mounted}
            <div
                class="container"
                in:fly={{
                    y: 30,
                    duration: 800,
                    delay: sectionDelay * 4,
                    easing: cubicOut,
                }}
            >
                <div class="section-header">
                    <h2>Why Choose Atomic</h2>
                    <div class="divider"></div>
                </div>

                <div class="stats-grid">
                    <div class="gradient-border-container">
                        <div class="gradient-border-content stat-card">
                            <span class="stat-num"
                                >{Math.floor(
                                    $usersCount,
                                ).toLocaleString()}+</span
                            >
                            <span class="stat-label">Trusted Users</span>
                        </div>
                    </div>
                    <div class="gradient-border-container">
                        <div class="gradient-border-content stat-card">
                            <span class="stat-num"
                                >{$transactionSpeed.toFixed(1)}%</span
                            >
                            <span class="stat-label">Uptime & Reliability</span>
                        </div>
                    </div>
                </div>
            </div>
        {/if}
    </section>

    <!-- 6. CTA -->
    <section class="section-light cta-section">
        {#if mounted}
            <div
                class="container"
                in:fly={{
                    y: 30,
                    duration: 800,
                    delay: sectionDelay * 5,
                    easing: cubicOut,
                }}
            >
                <div class="glass-panel cta-panel">
                    <h2 class="text-gradient">Ready to Bank Smarter?</h2>
                    <p>
                        Join the revolution today and take control of your
                        financial atoms.
                    </p>
                    <div style="margin-top: 2rem;">
                        <button
                            class="cta-primary"
                            on:click={() => navigate("register")}
                        >
                            <span>Open Account</span>
                            <div class="icon-wrapper">
                                <ArrowRight size={20} />
                            </div>
                        </button>
                    </div>
                </div>
            </div>
        {/if}
    </section>

    <!-- 7. CONTACT -->
    <section class="section-solid contact" id="contact">
        {#if mounted}
            <div
                class="container"
                in:fly={{
                    y: 30,
                    duration: 800,
                    delay: sectionDelay * 6,
                    easing: cubicOut,
                }}
            >
                <div class="section-header">
                    <h2>Contact</h2>
                    <div class="divider"></div>
                </div>
                <div class="contact-grid">
                    <div class="glass-panel contact-box">
                        <h4>Support</h4>
                        <p>help@atomicbank.com</p>
                    </div>
                    <div class="glass-panel contact-box">
                        <h4>Media</h4>
                        <p>press@atomicbank.com</p>
                    </div>
                </div>
            </div>
        {/if}
    </section>

    <!-- 8. FOOTER -->
    <footer>
        <div class="container footer-centered">
            <p>&copy; 2026 Atomic Bank. All rights reserved.</p>
            <div class="links">
                <span>Privacy</span>
                <span>Terms</span>
                <span>Security</span>
            </div>
        </div>
    </footer>
</main>

<style>
    .home-container {
        width: 100%;
        overflow-x: hidden;
    }

    section {
        padding: 6rem 2rem;
        width: 100%;
        display: flex;
        justify-content: center;
        perspective: 1000px;
    }

    .container {
        max-width: 1200px;
        width: 100%;
    }

    .section-solid {
        background: var(--bg-primary);
    }
    .section-light {
        background: var(--bg-secondary);
    }

    .section-header {
        text-align: center;
        margin-bottom: 4rem;
    }

    .section-header h2 {
        font-size: 2.5rem;
        margin-bottom: 1rem;
        color: var(--brand-dark);
    }

    .divider {
        width: 60px;
        height: 4px;
        background: linear-gradient(90deg, var(--brand-primary), var(--accent));
        margin: 0 auto;
        border-radius: 4px;
    }

    /* HERO */
    .hero {
        min-height: 100vh;
        align-items: center;
        background: var(--bg-primary);
        padding-top: 0;
    }

    .grid-2 {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 4rem;
        align-items: center;
    }

    .hero-text h1 {
        font-size: 4rem;
        line-height: 1.1;
        margin-bottom: 1.5rem;
        letter-spacing: -2px;
    }

    .hero-text p {
        font-size: 1.25rem;
        margin-bottom: 3rem;
        max-width: 90%;
    }

    .cta-primary {
        position: relative;
        display: inline-flex;
        align-items: center;
        gap: 1rem;
        padding: 1rem 2.5rem;
        background: var(--brand-dark);
        color: #fff;
        border: none;
        border-radius: 50px;
        font-size: 1.1rem;
        font-weight: 600;
        cursor: pointer;
        overflow: hidden;
        transition: all 0.3s cubic-bezier(0.25, 0.8, 0.25, 1);
        z-index: 10;
    }

    .cta-primary:hover {
        transform: translateY(-2px);
        box-shadow: 0 10px 20px rgba(52, 73, 94, 0.2);
        padding-right: 3.5rem;
    }

    .icon-wrapper {
        position: absolute;
        right: 1.5rem;
        opacity: 0;
        transform: translateX(-10px);
        transition: all 0.3s ease;
        display: flex;
        align-items: center;
    }

    .cta-primary:hover .icon-wrapper {
        opacity: 1;
        transform: translateX(0);
    }

    .hero-image {
        display: flex;
        justify-content: center;
    }

    .floating-card {
        width: 100%;
        max-width: 500px;
        height: auto;
        border-radius: 16px;
        animation: floatCard 6s ease-in-out infinite;
        filter: drop-shadow(0 20px 40px rgba(0, 0, 0, 0.1));
    }

    @keyframes floatCard {
        0% { transform: translateY(0px) rotateX(0deg); }
        50% { transform: translateY(-20px) rotateX(2deg); }
        100% { transform: translateY(0px) rotateX(0deg); }
    }

    .about-panel {
        padding: 4rem;
        text-align: center;
        font-size: 1.5rem;
        color: var(--brand-dark);
        font-weight: 300;
        max-width: 900px;
        margin: 0 auto;
        line-height: 1.6;
    }

    .grid-3 {
        display: grid;
        grid-template-columns: repeat(auto-fit, minmax(300px, 1fr));
        gap: 2rem;
    }

    .feature-card {
        padding: 2.5rem;
        text-align: left;
        display: flex;
        flex-direction: column;
        justify-content: flex-start;
        height: 100%;
    }

    .icon-box {
        margin-bottom: 1.5rem;
    }

    .feature-card h3 {
        font-size: 1.5rem;
        margin-bottom: 1rem;
        color: var(--brand-dark);
    }

    .steps-container {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
    }

    .step-card {
        flex: 1;
        padding: 2.5rem;
        text-align: center;
        border-radius: 20px;
        transition: transform 0.3s ease;
    }

    .step-card:hover {
        transform: translateY(-5px);
    }

    .step-num {
        font-size: 3rem;
        font-weight: 800;
        color: rgba(52, 152, 219, 0.2);
        margin-bottom: 1rem;
        line-height: 1;
    }

    .step-card h4 {
        margin-bottom: 0.5rem;
        font-size: 1.25rem;
    }

    .step-arrow {
        display: flex;
        align-items: center;
        opacity: 0.5;
    }

    .stats-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 4rem;
        max-width: 900px;
        margin: 0 auto;
    }

    .stat-card {
        padding: 3rem;
        text-align: center;
        display: flex;
        flex-direction: column;
        justify-content: center;
        align-items: center;
    }

    .stat-num {
        font-size: 3.5rem;
        font-weight: 700;
        color: var(--brand-primary);
        line-height: 1.2;
    }

    .stat-label {
        font-size: 1.1rem;
        text-transform: uppercase;
        letter-spacing: 1px;
        color: var(--text-muted);
        margin-top: 0.5rem;
    }

    .cta-panel {
        padding: 4rem;
        text-align: center;
        border-radius: 30px;
        max-width: 800px;
        margin: 0 auto;
    }

    .cta-panel h2 {
        font-size: 3rem;
        margin-bottom: 1rem;
    }

    .contact-grid {
        display: grid;
        grid-template-columns: 1fr 1fr;
        gap: 2rem;
        max-width: 800px;
        margin: 0 auto;
    }

    .contact-box {
        padding: 2rem;
        text-align: center;
        border-radius: 16px;
    }

    .contact-box h4 {
        margin-bottom: 0.5rem;
        font-size: 1.2rem;
    }

    footer {
        background: var(--bg-primary);
        padding: 4rem 2rem;
        border-top: 1px solid var(--bg-secondary);
        color: var(--text-muted);
    }

    .links {
        margin-top: 1.5rem;
        display: flex;
        gap: 2rem;
        font-size: 0.9rem;
    }

    .links span {
        cursor: pointer;
        transition: color 0.2s;
    }

    .links span:hover {
        color: var(--brand-primary);
    }

    @media (max-width: 900px) {
        .grid-2,
        .stats-grid,
        .contact-grid {
            grid-template-columns: 1fr;
            text-align: center;
        }

        .steps-container {
            flex-direction: column;
        }

        .step-arrow {
            display: none;
        }

        .hero-text h1 {
            font-size: 3rem;
        }
    }
</style>
