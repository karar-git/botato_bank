<script>
    import { onMount, createEventDispatcher } from "svelte";
    import { tweened } from "svelte/motion";
    import { cubicOut } from "svelte/easing";

    export let navigate;

    let y = 0;
    let visible = false;

    function handleScroll() {
        y = window.scrollY;
        if (y > window.innerHeight - 50) {
            visible = true;
        } else {
            visible = false;
        }
    }

    const navY = tweened(-150, {
        duration: 500,
        easing: cubicOut,
    });

    $: navY.set(visible ? 0 : -150);

    onMount(() => {
        handleScroll();
        window.addEventListener("scroll", handleScroll);
        return () => window.removeEventListener("scroll", handleScroll);
    });

    function scrollToTop() {
        window.scrollTo({ top: 0, behavior: "smooth" });
    }

    function scrollToSection(id) {
        const el = document.getElementById(id);
        if (el) el.scrollIntoView({ behavior: "smooth" });
    }
</script>

<nav class="navbar glass-nav" style="transform: translate(-50%, {$navY}%);">
    <div class="nav-content">
        <div
            class="logo"
            on:click={scrollToTop}
            on:keydown={scrollToTop}
            role="button"
            tabindex="0"
        >
            Atomic Bank
        </div>
        <div class="nav-links">
            <button on:click={() => scrollToSection("about")}>About Us</button>
            <button on:click={() => scrollToSection("contact")}>Contact</button>
            <button class="nav-login-btn" on:click={() => navigate("login")}>Login</button>
        </div>
    </div>
</nav>

<style>
    .navbar {
        position: fixed;
        top: 20px;
        left: 50%;
        width: auto;
        min-width: 450px;
        z-index: 1000;
        border-radius: 50px;
        padding: 0.75rem 2rem;
        display: flex;
        justify-content: center;
    }

    .nav-content {
        display: flex;
        align-items: center;
        justify-content: space-between;
        width: 100%;
        gap: 3rem;
    }

    .logo {
        font-weight: 700;
        font-size: 1.1rem;
        color: var(--brand-dark);
        cursor: pointer;
        user-select: none;
    }

    .nav-links {
        display: flex;
        gap: 1.5rem;
        align-items: center;
    }

    .nav-links button {
        background: none;
        border: none;
        font-size: 0.95rem;
        color: var(--text-muted);
        cursor: pointer;
        transition: color 0.2s ease;
        padding: 0;
    }

    .nav-links button:hover {
        color: var(--brand-primary);
    }

    .nav-login-btn {
        background: var(--brand-dark) !important;
        color: #fff !important;
        padding: 0.4rem 1.2rem !important;
        border-radius: 50px;
        font-weight: 600;
        font-size: 0.85rem !important;
        transition: all 0.3s ease !important;
    }

    .nav-login-btn:hover {
        transform: translateY(-1px);
        box-shadow: 0 4px 12px rgba(52, 73, 94, 0.3);
    }
</style>
